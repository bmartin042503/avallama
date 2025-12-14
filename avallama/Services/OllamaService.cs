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
using avallama.Dtos;
using avallama.Extensions;
using avallama.Models;
using avallama.Utilities;

namespace avallama.Services
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

        /// <summary>The service encountered a critical error and could not start.</summary>
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
        Task Start();

        /// <summary>
        /// Stops the local Ollama process and disposes of resources.
        /// </summary>
        void Stop();

        /// <summary>
        /// Retries the connection logic with an exponential backoff strategy.
        /// </summary>
        Task Retry();

        /// <summary>
        /// Checks if the Ollama API is reachable without modifying the internal service state.
        /// </summary>
        /// <returns>True if the server responds with HTTP 200 OK; otherwise, false.</returns>
        Task<bool> CheckConnectionAsync();

        // Model Management

        /// <summary>
        /// Retrieves the list of models currently downloaded to the local machine.
        /// </summary>
        /// <param name="forceRefresh">If true, bypasses the in-memory cache and fetches fresh data from the API.</param>
        /// <returns>A list of downloaded Ollama models.</returns>
        Task<IList<OllamaModel>> GetDownloadedModels(bool forceRefresh = false);

        /// <summary>
        /// Deletes a specific model from the local storage.
        /// </summary>
        /// <param name="modelName">The tag name of the model to delete.</param>
        /// <returns>True if the deletion was successful.</returns>
        Task<bool> DeleteModel(string modelName);

        /// <summary>
        /// Downloads (pulls) a model from the Ollama library.
        /// </summary>
        /// <param name="modelName">The name of the model to pull.</param>
        /// <returns>An async stream of download progress responses.</returns>
        IAsyncEnumerable<DownloadResponse> PullModel(string modelName);

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
        private readonly IOllamaScraperService _ollamaScraperService;
        private readonly IAvaloniaDispatcher _dispatcher;
        private readonly HttpClient _checkHttpClient;
        private readonly HttpClient _heavyHttpClient;

        // Internal State
        private Process? _ollamaProcess;
        private bool _started;

        /// <summary>
        /// Semaphore to ensure thread safety during the Start sequence.
        /// </summary>
        private readonly SemaphoreSlim _startLock = new(1, 1);

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
        public Func<ProcessStartInfo, Process?> StartProcessFunc { get; init; } = Process.Start;

        /// <summary>Delegate used to check running processes. Can be mocked for unit testing.</summary>
        public Func<int> GetProcessCountFunc { get; init; } = () => Process.GetProcessesByName("ollama").Length;

        public TimeSpan MaxRetryingTime { get; init; } = TimeSpan.FromSeconds(15);
        public TimeSpan ConnectionCheckInterval { get; init; } = TimeSpan.FromMilliseconds(500);

        private string OllamaPath { get; set; } = "";

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="OllamaService"/> class.
        /// </summary>
        /// <param name="configurationService">Service for reading app settings.</param>
        /// <param name="dialogService">Service for showing UI dialogs.</param>
        /// <param name="modelCacheService">Service for caching model details.</param>
        /// <param name="ollamaScraperService">Service for scraping online models.</param>
        /// <param name="dispatcher">Dispatcher to marshal events to the UI thread.</param>
        /// <param name="httpClientFactory">Factory to create a named HttpClient instance.</param>
        public OllamaService(
            IConfigurationService configurationService,
            IDialogService dialogService,
            IModelCacheService modelCacheService,
            IOllamaScraperService ollamaScraperService,
            IAvaloniaDispatcher dispatcher,
            IHttpClientFactory httpClientFactory)
        {
            _configurationService = configurationService;
            _dialogService = dialogService;
            _modelCacheService = modelCacheService;
            _ollamaScraperService = ollamaScraperService;
            _dispatcher = dispatcher;

            _checkHttpClient = httpClientFactory.CreateClient("OllamaCheckHttpClient");
            _heavyHttpClient = httpClientFactory.CreateClient("OllamaHeavyHttpClient");

            OllamaServiceState = new ServiceState(ServiceStatus.Stopped);
        }

        #endregion

        #region Lifecycle Methods

        /// <summary>
        /// Orchestrates the startup of the Ollama service.
        /// Checks for existing instances, attempts to start the process if needed, and verifies connectivity.
        /// </summary>
        public async Task Start()
        {
            await _startLock.WaitAsync();

            try
            {
                if (_started) return;

                ConfigureOllamaPath();

                // Check for existing instances
                var ollamaProcessCount = GetProcessCountFunc();
                switch (ollamaProcessCount)
                {
                    case 1:
                        OllamaServiceState = new ServiceState(ServiceStatus.Running,
                            LocalizationService.GetString("OLLAMA_ALREADY_RUNNING"));
                        _started = true;
                        return;
                    case >= 2:
                        OllamaServiceState = new ServiceState(ServiceStatus.Stopped,
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
                _startLock.Release();
            }
        }

        /// <summary>
        /// Stops the managed Ollama process and resets the internal state.
        /// </summary>
        public void Stop()
        {
            var ollamaProcessList = Process.GetProcessesByName("ollama");
            foreach (var process in ollamaProcessList)
            {
                KillProcess(process);
            }

            _started = false;
            OllamaServiceState = new ServiceState(ServiceStatus.Stopped);
        }

        /// <summary>
        /// Retries the connection to the Ollama API.
        /// </summary>
        public async Task Retry()
        {
            OllamaServiceState = new ServiceState(ServiceStatus.Retrying);

            var stopwatch = Stopwatch.StartNew();

            while (stopwatch.Elapsed < MaxRetryingTime)
            {
                if (await CheckConnectionAsync())
                {
                    OllamaServiceState = new ServiceState(ServiceStatus.Running,
                        LocalizationService.GetString("OLLAMA_STARTED"));
                    return;
                }

                await Task.Delay(ConnectionCheckInterval);
            }

            OllamaServiceState = new ServiceState(ServiceStatus.Stopped,
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
                using var response = await _checkHttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                return response.StatusCode == HttpStatusCode.OK;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region API Methods

        /// <inheritdoc />
        public async Task<IList<OllamaModel>> GetDownloadedModels(bool forceRefresh = false)
        {
            if (!await CheckConnectionAsync())
            {
                HandleConnectionError();
                return new List<OllamaModel>();
            }

            if (!forceRefresh && _downloadedModels != null && _downloadedModels.Any())
            {
                return _downloadedModels;
            }

            var tagsResponse = await FetchOllamaTagsAsync();
            if (tagsResponse?.Models == null) return [];

            var downloadedModels = new List<OllamaModel>();

            foreach (var tag in tagsResponse.Models.Where(m => !string.IsNullOrEmpty(m.Name)))
            {
                var downloadedModel = CreateModelFromTag(tag);
                try
                {
                    var showResponse = await FetchModelInfoAsync(downloadedModel.Name);
                    EnrichModelWithInfo(downloadedModel, showResponse);
                    downloadedModel.DownloadStatus = ModelDownloadStatus.Downloaded;
                }
                catch (Exception ex)
                {
                    // TODO: proper logging
                    Console.WriteLine($"Failed to get model info for {downloadedModel.Name}: {ex.Message}");
                }

                downloadedModels.Add(downloadedModel);
            }

            foreach (var model in downloadedModels)
            {
                await _modelCacheService.UpdateModelAsync(model);
            }

            _downloadedModels = downloadedModels;
            return downloadedModels;
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<DownloadResponse> PullModel(string modelName)
        {
            if (!await CheckConnectionAsync())
            {
                HandleConnectionError();
                yield break;
            }

            var payload = new { model = modelName, stream = true };
            var jsonPayload = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            using var request = CreateRequest(HttpMethod.Post, "/api/pull");
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
                HandleConnectionError();
                yield break;
            }

            await using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);

            while (await reader.ReadLineAsync() is { } line)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                DownloadResponse? json = null;
                try
                {
                    json = JsonSerializer.Deserialize<DownloadResponse>(line);
                }
                catch (JsonException e)
                {
                    _dialogService.ShowErrorDialog(e.Message, false);
                }

                if (json != null) yield return json;
            }
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<OllamaResponse> GenerateMessage(List<Message> messageHistory, string modelName)
        {
            if (!await CheckConnectionAsync())
            {
                HandleConnectionError();
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
                HandleConnectionError();
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
                HandleConnectionError();
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
            var result = await _ollamaScraperService.GetAllOllamaModelsAsync(cancellationToken);
            _currentScrapeSession = result;

            await foreach (var model in result.Models.WithCancellation(cancellationToken))
            {
                yield return model;
            }
        }

        #endregion

        #region Private Helpers

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
                _ollamaProcess = StartProcessFunc(startInfo);
                if (_ollamaProcess != null) return true;
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
            if (string.IsNullOrEmpty(host)) host = "localhost";

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
        private void HandleConnectionError()
        {
            OllamaServiceState = new ServiceState(ServiceStatus.Stopped,
                LocalizationService.GetString("OLLAMA_CONNECTION_ERROR"));
        }

        private static void KillProcess(Process? process)
        {
            if (process == null || process.HasExited) return;
            process.Kill();
            process.Dispose();
        }

        /// <summary>
        /// Marshals the state change event to the UI thread using the Dispatcher.
        /// </summary>
        private void NotifyStatusChanged(ServiceState? state)
        {
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
            _startLock.Dispose();
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
