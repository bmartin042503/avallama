// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using avallama.Constants.Keys;
using avallama.Exceptions;
using avallama.Models.Dtos;
using avallama.Extensions;
using avallama.Models;
using avallama.Models.Ollama;
using avallama.Services.Persistence;
using avallama.Utilities;
using avallama.Utilities.Network;
using avallama.Utilities.Time;

namespace avallama.Services.Ollama
{
    #region Supporting Types

    /// <summary>
    /// Represents the various operational states of the Ollama service.
    /// </summary>
    public enum ServiceStatus
    {
        /// <summary>The Ollama executable was not found on the system.</summary>
        NotInstalled,

        /// <summary>The service is currently not running.</summary>
        Stopped,

        /// <summary>The service is attempting to establish a connection.</summary>
        Retrying,

        /// <summary>The service is up, running, and responding to requests.</summary>
        Running,

        /// <summary>The service encountered a critical error and could not function.</summary>
        Failed
    }

    /// <summary>
    /// Represents the current state of the service, including the status enum and a descriptive message.
    /// </summary>
    /// <param name="Status">The current operational status.</param>
    /// <param name="Message">An optional localized message describing the state.</param>
    public record ServiceState(ServiceStatus Status, string? Message = null);

    /// <summary>
    /// Delegate for handling service state change events.
    /// </summary>
    /// <param name="state">The new state of the service.</param>
    public delegate void ServiceStateChangedHandler(ServiceState? state);

    #endregion

    /// <summary>
    /// Defines the contract for managing the local Ollama process and interacting with its API.
    /// </summary>
    public interface IOllamaService
    {
        #region Interface

        // Properties & Events

        /// <summary>
        /// Gets the current state of the Ollama service.
        /// </summary>
        ServiceState? OllamaServiceState { get; }

        /// <summary>
        /// Event triggered when the service state changes (e.g., from Starting to Running).
        /// </summary>
        event ServiceStateChangedHandler? ServiceStateChanged;

        // Lifecycle

        /// <summary>
        /// Attempts to start the Ollama process and establish a connection.
        /// Thread-safe and prevents multiple concurrent start attempts.
        /// </summary>
        Task StartAsync();

        /// <summary>
        /// Stops the local Ollama process and disposes of resources.
        /// </summary>
        Task StopAsync();

        /// <summary>
        /// Retries the connection logic with an exponential backoff strategy.
        /// </summary>
        Task Retry();

        /// <summary>
        /// Checks if the Ollama API is reachable without modifying the internal service state.
        /// </summary>
        /// <returns>True if the server responds with HTTP 200 OK; otherwise, false.</returns>
        Task<bool> CheckConnectionAsync();

        /// <summary>
        /// Asynchronously waits until the Ollama service enters the <see cref="ServiceStatus.Running"/> state.
        /// If the service is already running, the task completes immediately.
        /// </summary>
        Task WaitForRunningAsync();

        /// <summary>
        /// Retrieves the list of downloaded models.
        /// <para>
        /// This method uses the in-memory cache if available. If the cache is empty,
        /// it automatically triggers a synchronization via <see cref="UpdateDownloadedModels"/>.
        /// </para>
        /// </summary>
        /// <returns>A list of models available locally, or an empty list if the service is unreachable.</returns>
        Task<IList<OllamaModel>> GetDownloadedModels();

        /// <summary>
        /// Forces a synchronization with the local Ollama API.
        /// <para>
        /// This method fetches fresh data from the API, enriches it with model details,
        /// and updates both the persistent storage and the in-memory cache.
        /// </para>
        /// </summary>
        Task UpdateDownloadedModels();

        /// <summary>
        /// Deletes a specific model from the local Ollama.
        /// </summary>
        /// <param name="modelName">The tag name of the model to delete.</param>
        /// <returns>True if the deletion was successful.</returns>
        Task<bool> DeleteModel(string modelName);

        /// <summary>
        /// Downloads (pulls) a model from the Ollama library.
        /// </summary>
        /// <param name="modelName">The name of the model to pull.</param>
        /// <param name="ct">
        /// The cancellation token to cancel the operation.
        /// Decorated with <see cref="EnumeratorCancellationAttribute"/> to ensure the token is passed correctly
        /// when using <c>await foreach</c> loop cancellation or <c>WithCancellation</c>.
        /// Cancelling this token will abort the underlying HTTP request immediately.
        /// </param>
        /// <returns>An async stream of download progress responses.</returns>
        IAsyncEnumerable<DownloadResponse> PullModelAsync(
            string modelName,
            CancellationToken ct = default
        );

        // Chat & Scraper

        /// <summary>
        /// Generates a chat response from the specified model based on the message history.
        /// </summary>
        /// <param name="messageHistory">The history of messages in the conversation.</param>
        /// <param name="modelName">The model to use for generation.</param>
        /// <returns>An async stream of response chunks.</returns>
        IAsyncEnumerable<OllamaResponse> GenerateMessage(List<Message> messageHistory, string modelName);

        /// <summary>
        /// Retrieves model families scraped from the Ollama website.
        /// </summary>
        Task<IList<OllamaModelFamily>> GetScrapedFamiliesAsync();

        /// <summary>
        /// Streams all available models scraped from the Ollama website.
        /// </summary>
        IAsyncEnumerable<OllamaModel> StreamAllScrapedModelsAsync(CancellationToken cancellationToken);

        #endregion
    }

    /// <summary>
    /// Implementation of the Ollama service. Manages the local 'ollama serve' process,
    /// handles HTTP communication, and manages application state related to the LLM backend.
    /// </summary>
    public class OllamaService : IOllamaService, IDisposable
    {
        #region Constants & Fields

        public const int DefaultApiPort = 11434;
        public const string DefaultApiHost = "localhost";

        // Dependencies
        private readonly IConfigurationService _configurationService;
        private readonly IDialogService _dialogService;
        private readonly IModelCacheService _modelCacheService;
        private readonly IOllamaScraper _ollamaScraper;
        private readonly INetworkManager _networkManager;
        private readonly IAvaloniaDispatcher _dispatcher;
        private readonly HttpClient _checkHttpClient;
        private readonly HttpClient _heavyHttpClient;
        private readonly ITimeProvider _timeProvider;
        private readonly ITaskDelayer _taskDelayer;

        // Internal State
        private IOllamaProcess? _process;
        private bool _started;

        /// <summary>
        /// Semaphore to ensure thread safety during the Start sequence.
        /// </summary>
        private readonly SemaphoreSlim _processSemaphore = new(1, 1);

        // Caching
        private OllamaScraperResult? _currentScrapeSession;
        private IList<OllamaModel>? _downloadedModels;
        private readonly JsonSerializerOptions _jsonSerializerOptions = new() { PropertyNameCaseInsensitive = true };

        #endregion

        #region Properties & Events

        /// <summary>
        /// Gets the current operational state of the service.
        /// Setting this property automatically triggers the <see cref="ServiceStateChanged"/> event on the UI thread.
        /// </summary>
        public ServiceState? OllamaServiceState
        {
            get;
            private set
            {
                if (field == value) return;
                field = value;
                NotifyStatusChanged(value);
            }
        }

        public event ServiceStateChangedHandler? ServiceStateChanged;

        // Mockable delegates for testing

        /// <summary>Delegate used to start the process. Can be mocked for unit testing.</summary>
        public Func<ProcessStartInfo, IOllamaProcess?> StartProcessFunc { get; init; } = psi =>
        {
            var process = Process.Start(psi);
            return process != null ? new OllamaProcess(process) : null;
        };

        /// <summary>Delegate used to retrieve running processes. Can be mocked for unit testing.</summary>
        public Func<IEnumerable<IOllamaProcess>> GetProcessesFunc { get; init; }

        /// <summary>Delegate used to check if Ollama is running as a systemd service (Linux only). Can be mocked for unit testing.</summary>
        public Func<bool> CheckSystemdStatusFunc { get; init; }

        public TimeSpan MaxRetryingTime { get; init; } = TimeSpan.FromSeconds(15);
        public TimeSpan ConnectionCheckInterval { get; init; } = TimeSpan.FromMilliseconds(500);

        private string OllamaPath { get; set; } = "";

        private TaskCompletionSource<bool> _serverStartedTcs = new();

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="OllamaService"/> class.
        /// </summary>
        /// <param name="configurationService">Service for reading app settings.</param>
        /// <param name="dialogService">Service for showing UI dialogs.</param>
        /// <param name="modelCacheService">Service for caching model details.</param>
        /// <param name="ollamaScraper">Service for scraping online models.</param>
        /// <param name="networkManager">Network manager to perform connection checks.</param>
        /// <param name="dispatcher">Dispatcher to marshal events to the UI thread.</param>
        /// <param name="httpClientFactory">Factory to create a named HttpClient instance.</param>
        /// <param name="timeProvider">Time provider to use for time measurements.</param>
        /// <param name="taskDelayer">Task delayer to use for adding delays.</param>
        public OllamaService(
            IConfigurationService configurationService,
            IDialogService dialogService,
            IModelCacheService modelCacheService,
            IOllamaScraper ollamaScraper,
            INetworkManager networkManager,
            IAvaloniaDispatcher dispatcher,
            IHttpClientFactory httpClientFactory,
            ITimeProvider? timeProvider = null,
            ITaskDelayer? taskDelayer = null)
        {
            GetProcessesFunc ??= () => Process.GetProcessesByName("ollama").Select(p => new OllamaProcess(p));
            CheckSystemdStatusFunc ??= IsOllamaRunningAsSystemdService;

            _configurationService = configurationService;
            _dialogService = dialogService;
            _modelCacheService = modelCacheService;
            _ollamaScraper = ollamaScraper;
            _networkManager = networkManager;
            _dispatcher = dispatcher;

            _checkHttpClient = httpClientFactory.CreateClient("OllamaCheckHttpClient");
            _heavyHttpClient = httpClientFactory.CreateClient("OllamaHeavyHttpClient");

            _timeProvider = timeProvider ?? new RealTimeProvider();
            _taskDelayer = taskDelayer ?? new RealTaskDelayer();

            OllamaServiceState = new ServiceState(ServiceStatus.Stopped);
        }

        #endregion

        #region Lifecycle Methods

        /// <summary>
        /// Orchestrates the startup of the Ollama service.
        /// Checks for existing instances, attempts to start the process if needed, and verifies connectivity.
        /// </summary>
        public async Task StartAsync()
        {
            await _processSemaphore.WaitAsync();

            try
            {
                if (_started) return;

                ConfigureOllamaPath();

                // check for existing instances (real server processes).
                // we get all processes named "ollama" and ensure they don't have a window handle
                // (to distinguish from GUI wrappers).
                var ollamaServerProcesses = GetProcessesFunc()
                    .Where(p => p.MainWindowHandle == IntPtr.Zero)
                    .Where(p => !p.ProcessName.Equals("Ollama", StringComparison.Ordinal))
                    .ToArray();

                if (ollamaServerProcesses.Length == 1)
                {
                    // attach to the existing process
                    // _isProcessStartedByAvallama = false;
                    _process = ollamaServerProcesses.FirstOrDefault();

                    // TODO: subscribe to Exited event and handle unexpected shutdown

                    OllamaServiceState = new ServiceState(ServiceStatus.Running,
                        LocalizationService.GetString("OLLAMA_ALREADY_RUNNING"));
                    return;
                }
                else if (ollamaServerProcesses.Length > 1)
                {
                    // multiple instances found, report failure rather than attempting to kill them
                    OllamaServiceState = new ServiceState(ServiceStatus.Failed,
                        LocalizationService.GetString("MULTIPLE_INSTANCES_ERROR"));
                    return;
                }

                // Attempt to start process
                if (!TryStartOllamaProcess()) return;

                // Verify connection
                if (await CheckConnectionAsync())
                {
                    OllamaServiceState =
                        new ServiceState(ServiceStatus.Running, LocalizationService.GetString("OLLAMA_STARTED"));
                    _started = true;
                }
                else
                {
                    // Fallback retry for slower systems
                    await Retry();
                }
            }
            finally
            {
                _processSemaphore.Release();
            }
        }

        /// <summary>
        /// Stops the managed Ollama process and resets the internal state.
        /// </summary>
        public async Task StopAsync()
        {
            await _processSemaphore.WaitAsync();

            // TODO: check if process is started by Avallama

            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && CheckSystemdStatusFunc())
                {
                    // ollama is running as a systemd service, so it should not be killed
                    return;
                }

                // TODO: better process exit handling, awaiting exit async, etc.

                var ollamaProcessList = Process.GetProcessesByName("ollama");
                foreach (var process in ollamaProcessList)
                {
                    if (process.HasExited) return;
                    process.Kill();
                    process.Dispose();
                }

                _started = false;
                OllamaServiceState = new ServiceState(ServiceStatus.Stopped);
            }
            finally
            {
                _processSemaphore.Release();
            }
        }

        /// <summary>
        /// Retries the connection to the Ollama API.
        /// If the connection fails initially, it checks if the Ollama process is running.
        /// If not, it attempts to start it and retries the connection.
        /// </summary>
        public async Task Retry()
        {
            OllamaServiceState = new ServiceState(ServiceStatus.Retrying);
            _timeProvider.Start();

            async Task<bool> WaitForConnection(TimeSpan timeout)
            {
                var loopStartTime = _timeProvider.Elapsed;
                while (_timeProvider.Elapsed - loopStartTime < timeout)
                {
                    if (await CheckConnectionAsync()) return true;
                    await _taskDelayer.Delay(ConnectionCheckInterval);
                }

                return false;
            }

            if (await WaitForConnection(MaxRetryingTime))
            {
                OllamaServiceState = new ServiceState(ServiceStatus.Running,
                    LocalizationService.GetString("OLLAMA_STARTED"));
                return;
            }

            var processCount = GetProcessesFunc().Count();

            // if there is no ollama process start one and restart connection checks
            if (processCount == 0)
            {
                ConfigureOllamaPath();

                if (TryStartOllamaProcess())
                {
                    _started = true;

                    if (await WaitForConnection(MaxRetryingTime))
                    {
                        OllamaServiceState = new ServiceState(ServiceStatus.Running,
                            LocalizationService.GetString("OLLAMA_STARTED"));
                        return;
                    }
                }
            }

            OllamaServiceState = new ServiceState(ServiceStatus.Failed,
                LocalizationService.GetString("OLLAMA_CONNECTION_ERROR"));
        }

        /// <summary>
        /// Performs a lightweight check against the /api/version endpoint to see if the server is responsive.
        /// This method does not alter the service state.
        /// </summary>
        /// <returns>True if the server responds with 200 OK; otherwise, false.</returns>
        public async Task<bool> CheckConnectionAsync()
        {
            try
            {
                using var request = CreateRequest(HttpMethod.Get, "/api/version");
                using var response =
                    await _checkHttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                return response.StatusCode == HttpStatusCode.OK;
            }
            catch
            {
                return false;
            }
        }

        public Task WaitForRunningAsync()
        {
            return OllamaServiceState?.Status == ServiceStatus.Running ? Task.CompletedTask : _serverStartedTcs.Task;
        }

        #endregion

        #region API Methods

        /// <inheritdoc />
        public async Task<IList<OllamaModel>> GetDownloadedModels()
        {
            if (!await CheckConnectionAsync())
            {
                SetFailedServiceStatus();
                return new List<OllamaModel>();
            }

            if (_downloadedModels == null || _downloadedModels.Count == 0)
            {
                await UpdateDownloadedModels();
            }

            return _downloadedModels ?? new List<OllamaModel>();
        }

        /// <inheritdoc />
        public async Task UpdateDownloadedModels()
        {
            if (!await CheckConnectionAsync())
            {
                SetFailedServiceStatus();
                return;
            }

            var tagsResponse = await FetchOllamaTagsAsync();
            if (tagsResponse?.Models == null) return;

            var downloadedModels = new System.Collections.Concurrent.ConcurrentBag<OllamaModel>();

            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = 5
            };

            await Parallel.ForEachAsync(tagsResponse.Models.Where(m => !string.IsNullOrEmpty(m.Name)), parallelOptions,
                async (tag, _) =>
                {
                    var downloadedModel = CreateModelFromTag(tag);
                    try
                    {
                        var showResponse = await FetchModelInfoAsync(downloadedModel.Name);
                        EnrichModelWithInfo(downloadedModel, showResponse);
                        downloadedModel.IsDownloaded = true;
                    }
                    catch (Exception)
                    {
                        // TODO: proper logging
                    }

                    downloadedModels.Add(downloadedModel);
                });

            var sortedModels = downloadedModels.OrderBy(m => m.Name).ToList();

            // TODO: fix local model caching when there is no scraped models in the db
            foreach (var model in sortedModels)
            {
                await _modelCacheService.UpdateModelAsync(model);
            }

            _downloadedModels = sortedModels;
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<DownloadResponse> PullModelAsync(
            string modelName,
            [EnumeratorCancellation] CancellationToken ct = default)
        {
            if (!await CheckConnectionAsync())
            {
                SetFailedServiceStatus();
                ThrowServiceUnreachableException();
            }

            var payload = new { model = modelName, stream = true };
            var jsonPayload = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            using var request = CreateRequest(HttpMethod.Post, "/api/pull");
            request.Content = content;

            HttpResponseMessage response;
            try
            {
                response = await _heavyHttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            }
            catch (HttpRequestException ex)
            {
                ThrowServiceUnreachableException(ex);
                throw; // never runs but needed for compiler when initializing
            }

            if (response.StatusCode == HttpStatusCode.OK)
            {
                OllamaServiceState = new ServiceState(ServiceStatus.Running);
            }
            else
            {
                throw new OllamaApiException(response.StatusCode);
            }

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var reader = new StreamReader(stream);

            // TODO: make this more readable
            while (true)
            {
                string? line;
                try
                {
                    line = await reader.ReadLineAsync(ct);
                }
                catch (IOException ex)
                {
                    if (!await _networkManager.IsInternetAvailableAsync())
                    {
                        throw new LostInternetConnectionException(ex);
                    }

                    throw;
                }

                if (line == null) break;

                if (string.IsNullOrWhiteSpace(line)) continue;

                DownloadResponse? json = null;
                try
                {
                    json = JsonSerializer.Deserialize<DownloadResponse>(line, _jsonSerializerOptions);
                }
                catch (JsonException)
                {
                    // TODO: proper logging
                }

                if (json != null) yield return json;
            }
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<OllamaResponse> GenerateMessage(List<Message> messageHistory, string modelName)
        {
            if (!await CheckConnectionAsync())
            {
                SetFailedServiceStatus();
                yield break;
            }

            var chatRequest = new ChatRequest(messageHistory, modelName);
            var jsonPayload = chatRequest.ToJson();
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            using var request = CreateRequest(HttpMethod.Post, "/api/chat");
            request.Content = content;

            HttpResponseMessage response;
            try
            {
                response = await _heavyHttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    OllamaServiceState = new ServiceState(ServiceStatus.Running);
                }
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                SetFailedServiceStatus();
                yield break;
            }

            await using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);
            while (await reader.ReadLineAsync() is { } line)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                OllamaResponse? json = null;
                try
                {
                    json = JsonSerializer.Deserialize<OllamaResponse>(line);
                }
                catch (JsonException e)
                {
                    _dialogService.ShowErrorDialog(e.Message, false);
                }

                if (json != null) yield return json;
            }
        }

        /// <inheritdoc />
        public async Task<bool> DeleteModel(string modelName)
        {
            if (!await CheckConnectionAsync())
            {
                SetFailedServiceStatus();
                // TODO: throw ollama exception instead of returning boolean value
                return false;
            }

            var payload = new { model = modelName };
            var jsonPayload = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            using var request = CreateRequest(HttpMethod.Delete, "/api/delete");
            request.Content = content;

            using var response = await _heavyHttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            return response.StatusCode == HttpStatusCode.OK;
        }

        /// <inheritdoc />
        public Task<IList<OllamaModelFamily>> GetScrapedFamiliesAsync()
        {
            if (_currentScrapeSession?.Families is not { } families)
                return Task.FromResult<IList<OllamaModelFamily>>([]);
            _currentScrapeSession = null;
            return Task.FromResult(families);
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<OllamaModel> StreamAllScrapedModelsAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            _currentScrapeSession = null;
            var result = await _ollamaScraper.GetAllOllamaModelsAsync(cancellationToken);
            _currentScrapeSession = result;

            await foreach (var model in result.Models.WithCancellation(cancellationToken))
            {
                yield return model;
            }
        }

        #endregion

        #region Private Helpers

        /// <summary>
        /// Checks if the configured API host points to a remote server.
        /// </summary>
        private bool IsConnectionRemote()
        {
            var host = _configurationService.ReadSetting(ConfigurationKey.ApiHost);

            if (string.IsNullOrEmpty(host)) return false;

            return !host.Equals("localhost", StringComparison.OrdinalIgnoreCase) &&
                   !host.Equals("127.0.0.1", StringComparison.Ordinal);
        }

        /// <summary>
        /// Throws the appropriate exception based on whether the connection is local or remote.
        /// </summary>
        private void ThrowServiceUnreachableException(Exception? innerException = null)
        {
            if (IsConnectionRemote())
            {
                throw new OllamaRemoteServerUnreachableException(innerException);
            }

            throw new OllamaLocalServerUnreachableException(innerException);
        }

        /// <summary>
        /// Determines the appropriate path for the Ollama executable based on the operating system.
        /// </summary>
        private void ConfigureOllamaPath()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                OllamaPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    @"Programs\Ollama\ollama");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
                     RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                OllamaPath = @"/usr/local/bin/ollama";
            }
        }

        /// <summary>
        /// Determines whether Ollama is running as a systemd service or not
        /// </summary>
        /// <returns>True if running as systemd service, false otherwise</returns>
        /// <exception cref="InvalidOperationException">Thrown if called on a platform that is not Linux</exception>
        private static bool IsOllamaRunningAsSystemdService()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                throw new InvalidOperationException("This method should only be called on Linux");
            }

            var psiRoot = new ProcessStartInfo
            {
                FileName = "systemctl",
                Arguments = "is-active ollama",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            var psiUser = new ProcessStartInfo
            {
                FileName = "systemctl",
                Arguments = "--user is-active ollama",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            try
            {
                using var processRoot = Process.Start(psiRoot);
                if (processRoot == null)
                {
                    return false;
                }

                processRoot.WaitForExit();

                var outputRoot = processRoot.StandardOutput.ReadToEnd().Trim();
                if (outputRoot == "active")
                {
                    return true;
                }

                // retry with user level check if root check failed
                using var processUser = Process.Start(psiUser);
                if (processUser == null)
                {
                    return false;
                }

                processUser.WaitForExit();

                var outputUser = processUser.StandardOutput.ReadToEnd().Trim();
                return outputUser == "active";
            }
            catch (Win32Exception ex) when (ex.NativeErrorCode == 2)
            {
                // systemctl command failed because it does not exist, system does not use systemd
                return false;
            }
            catch (Win32Exception)
            {
                // other errors such as permission denied, should be handled in a different manner in the future
                return false;
            }
        }

        /// <summary>
        /// Attempts to launch the Ollama process with the 'serve' argument.
        /// </summary>
        /// <returns>True if the process started successfully; otherwise, false.</returns>
        private bool TryStartOllamaProcess()
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = OllamaPath,
                Arguments = "serve",
                UseShellExecute = false,
                CreateNoWindow = true
            };

            try
            {
                _process = StartProcessFunc(startInfo);
                if (_process != null) return true;
                OllamaServiceState = new ServiceState(ServiceStatus.NotInstalled);
                return false;
            }
            catch (Win32Exception)
            {
                OllamaServiceState = new ServiceState(ServiceStatus.NotInstalled);
                return false;
            }
            catch (Exception ex)
            {
                OllamaServiceState = new ServiceState(ServiceStatus.Failed,
                    string.Format(LocalizationService.GetString("OLLAMA_FAILED"), ex.Message));
                return false;
            }
        }

        /// <summary>
        /// Creates an HttpRequestMessage with the correct base URI derived from the configuration.
        /// </summary>
        private HttpRequestMessage CreateRequest(HttpMethod method, string endpoint)
        {
            var host = _configurationService.ReadSetting(ConfigurationKey.ApiHost);
            if (string.IsNullOrEmpty(host) || host == "localhost")
            {
                // use IPv4 since Windows may resolve 'localhost' to IPv6
                host = "127.0.0.1";
            }

            var port = _configurationService.ReadSetting(ConfigurationKey.ApiPort);
            if (string.IsNullOrEmpty(port)) port = "11434";

            var builder = new UriBuilder("http", host, int.Parse(port), endpoint);

            return new HttpRequestMessage(method, builder.Uri);
        }

        private async Task<OllamaTagsResponse?> FetchOllamaTagsAsync()
        {
            using var request = CreateRequest(HttpMethod.Get, "/api/tags");
            using var response = await _heavyHttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<OllamaTagsResponse>(json, _jsonSerializerOptions);
        }

        private async Task<OllamaShowResponse?> FetchModelInfoAsync(string modelName)
        {
            var payload = new { model = modelName };
            var jsonPayload = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            using var request = CreateRequest(HttpMethod.Post, "/api/show");
            request.Content = content;
            using var response = await _heavyHttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            var json = await response.Content.ReadAsStringAsync();

            return JsonSerializer.Deserialize<OllamaShowResponse>(json, _jsonSerializerOptions);
        }

        private OllamaModel CreateModelFromTag(OllamaModelDto tag)
        {
            var model = new OllamaModel { Name = tag.Name ?? string.Empty };

            if (tag.Size.HasValue) model.Size = tag.Size.Value;
            if (tag.Details == null) return model;

            if (tag.Details.QuantizationLevel != null)
                model.Info.TryAdd("quantization_level", tag.Details.QuantizationLevel);
            if (tag.Details.Format != null) model.Info.TryAdd("format", tag.Details.Format);

            return model;
        }

        private void EnrichModelWithInfo(OllamaModel model, OllamaShowResponse? showResponse)
        {
            if (showResponse?.ModelInfo == null) return;

            if (!string.IsNullOrEmpty(showResponse.License))
            {
                model.Info.TryAdd(ModelInfoKey.License, showResponse.License);
            }

            var info = showResponse.ModelInfo;
            if (!info.TryGetValue("general.architecture", out var archElem) ||
                !archElem.TryGetString(out var arch) ||
                string.IsNullOrEmpty(arch))
            {
                return;
            }

            model.Info.TryAdd(ModelInfoKey.Architecture, arch);

            if (info.TryGetValue("general.parameter_count", out var paramElem) &&
                paramElem.TryGetInt64(out var paramCount) && paramCount > 0)
            {
                model.Parameters = paramCount;
            }

            string[] keys = [ModelInfoKey.BlockCount, ModelInfoKey.ContextLength, ModelInfoKey.EmbeddingLength];
            foreach (var key in keys)
            {
                var searchKey = $"{arch}.{key}";
                if (info.TryGetValue(searchKey, out var element) && element.TryGetInt32(out var value) && value > 0)
                {
                    model.Info.TryAdd(key, value.ToString());
                }
            }
        }

        /// <summary>
        /// Updates the service state to Stopped/ConnectionError when an API call fails.
        /// </summary>
        private void SetFailedServiceStatus()
        {
            OllamaServiceState = new ServiceState(ServiceStatus.Failed,
                LocalizationService.GetString("OLLAMA_CONNECTION_ERROR"));
        }

        /// <summary>
        /// Marshals the state change event to the UI thread using the Dispatcher.
        /// </summary>
        private void NotifyStatusChanged(ServiceState? state)
        {
            switch (state?.Status)
            {
                case ServiceStatus.Running:
                    _serverStartedTcs.TrySetResult(true);
                    break;
                case ServiceStatus.Stopped or ServiceStatus.Failed:
                    // reset server started Task
                    if (!_serverStartedTcs.Task.IsCompleted)
                    {
                        _serverStartedTcs.TrySetResult(false);
                    }

                    _serverStartedTcs = new TaskCompletionSource<bool>();
                    break;
            }

            if (_dispatcher.CheckAccess())
            {
                ServiceStateChanged?.Invoke(state);
            }
            else
            {
                _dispatcher.Post(() => ServiceStateChanged?.Invoke(state));
            }
        }

        #endregion

        #region Dispose

        /// <summary>
        /// Disposes of the semaphore and suppresses finalization.
        /// </summary>
        public void Dispose()
        {
            _processSemaphore.Dispose();
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
