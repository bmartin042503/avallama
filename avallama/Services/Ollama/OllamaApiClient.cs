// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using avallama.Constants.Keys;
using avallama.Constants.States;
using avallama.Exceptions;
using avallama.Models;
using avallama.Models.Dtos;
using avallama.Models.Ollama;
using avallama.Services.Persistence;
using avallama.Utilities.Network;
using avallama.Utilities.Time;

namespace avallama.Services.Ollama;

/// <summary>
/// Delegate for handling changes in the Ollama API connection status.
/// </summary>
public delegate void OllamaApiStatusChangedHandler(OllamaApiStatus status);

/// <summary>
/// Defines the contract for interacting with the Ollama API, including connection management,
/// model retrieval, generation, and deletion.
/// </summary>
public interface IOllamaApiClient
{
    #region Interface

    /// <summary>
    /// Gets the current status of the connection to the Ollama API.
    /// </summary>
    OllamaApiStatus Status { get; }

    /// <summary>
    /// Event raised when the API connection status changes.
    /// </summary>
    event OllamaApiStatusChangedHandler? StatusChanged;

    /// <summary>
    /// Checks the connection to the Ollama API and updates the status accordingly.
    /// </summary>
    Task CheckConnectionAsync();

    /// <summary>
    /// Attempts to reconnect to the Ollama API within a configured timeout period.
    /// </summary>
    Task RetryConnectionAsync();

    /// <summary>
    /// Enriches the specified model with detailed information from the API (/api/tags, /api/show).
    /// </summary>
    /// <param name="model">The model to enrich.</param>
    Task EnrichModelAsync(OllamaModel model);

    /// <summary>
    /// Retrieves a list of models that are currently downloaded and available on the Ollama server.
    /// </summary>
    /// <returns>A list of downloaded models.</returns>
    Task<IList<OllamaModel>> GetDownloadedModelsAsync();

    /// <summary>
    /// Deletes a specific model from the Ollama server.
    /// </summary>
    /// <param name="modelName">The name of the model to delete.</param>
    /// <returns>True if the deletion was successful; otherwise, false.</returns>
    Task<bool> DeleteModelAsync(string modelName);

    /// <summary>
    /// Pulls a model from the Ollama library, streaming the download progress.
    /// </summary>
    /// <param name="modelName">The name of the model to download.</param>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>An async stream of download progress updates.</returns>
    IAsyncEnumerable<DownloadResponse> PullModelAsync(string modelName, CancellationToken ct = default);

    /// <summary>
    /// Generates a chat response from the specified model based on the message history.
    /// </summary>
    /// <param name="messageHistory">The history of messages in the conversation.</param>
    /// <param name="modelName">The name of the model to use for generation.</param>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>An async stream of response chunks.</returns>
    IAsyncEnumerable<OllamaResponse> GenerateMessageAsync(List<Message> messageHistory, string modelName,
        CancellationToken ct = default);

    #endregion
}

/// <summary>
/// Implementation of the Ollama API client, handling HTTP communication with the Ollama server.
/// </summary>
public class OllamaApiClient(
    IConfigurationService configurationService,
    INetworkManager networkManager,
    IHttpClientFactory httpClientFactory,
    ITimeProvider? timeProvider = null,
    ITaskDelayer? taskDelayer = null)
    : IOllamaApiClient
{
    #region Constants & Fields

    // Default server configuration
    public const int DefaultApiPort = 11434;
    public const string DefaultApiHost = "localhost";

    // Dependencies
    private readonly HttpClient _checkHttpClient = httpClientFactory.CreateClient("OllamaCheckHttpClient");
    private readonly HttpClient _heavyHttpClient = httpClientFactory.CreateClient("OllamaHeavyHttpClient");
    private readonly ITimeProvider _timeProvider = timeProvider ?? new RealTimeProvider();
    private readonly ITaskDelayer _taskDelayer = taskDelayer ?? new RealTaskDelayer();

    // Configuration
    private readonly TimeSpan _downloadTimeout = TimeSpan.FromSeconds(5);
    public TimeSpan MaxRetryingTime { get; init; } = TimeSpan.FromSeconds(15);
    public TimeSpan ConnectionCheckInterval { get; init; } = TimeSpan.FromMilliseconds(500);

    private readonly JsonSerializerOptions _jsonSerializerOptions = new() { PropertyNameCaseInsensitive = true };

    #endregion

    #region Event & Status

    public event OllamaApiStatusChangedHandler? StatusChanged;

    public OllamaApiStatus Status
    {
        get;
        private set
        {
            if (field == value) return;
            field = value;
            StatusChanged?.Invoke(value);
        }
    } = new(OllamaApiState.Disconnected);

    #endregion

    #region Public Methods

    public async Task CheckConnectionAsync()
    {
        Status = new OllamaApiStatus(OllamaApiState.Connecting);
        if (await IsOllamaReachable())
        {
            Status = new OllamaApiStatus(OllamaApiState.Connected);
        }
        else
        {
            await RetryConnectionAsync();
        }
    }

    public async Task RetryConnectionAsync()
    {
        Status = new OllamaApiStatus(OllamaApiState.Reconnecting);
        _timeProvider.Start();

        var loopStartTime = _timeProvider.Elapsed;
        while (_timeProvider.Elapsed - loopStartTime < MaxRetryingTime)
        {
            if (await IsOllamaReachable())
            {
                Status = new OllamaApiStatus(OllamaApiState.Connected);
                return;
            }

            await _taskDelayer.Delay(ConnectionCheckInterval);
        }

        SetUnreachableStatus();
    }

    public async Task<IList<OllamaModel>> GetDownloadedModelsAsync()
    {
        if (!await IsOllamaReachable())
        {
            SetUnreachableStatus();
            return [];
        }

        var tagsResponse = await FetchOllamaTagsAsync();

        var downloadedModels = tagsResponse?.Models
            .Where(dto => !string.IsNullOrEmpty(dto.Name))
            .Select(dto => new OllamaModel
            {
                Name = dto.Name!,
                Info = new Dictionary<string, string>()
            }).ToList();

        return downloadedModels ?? [];
    }

    public async Task EnrichModelAsync(OllamaModel model)
    {
        if (string.IsNullOrEmpty(model.Name)) return;

        if (!await IsOllamaReachable())
        {
            SetUnreachableStatus();
            return;
        }

        var tagsResponse = await FetchOllamaTagsAsync();

        var modelDto = tagsResponse?.Models.FirstOrDefault(modelDto => model.Name == modelDto.Name);

        if (modelDto == null) return;
        var modelShowResponse = await FetchModelInfoAsync(model.Name);

        // TODO: add missing enrich logic
        // model.EnrichWith(modelDto);
        // model.EnrichWith(modelShowResponse);
    }

    public async IAsyncEnumerable<DownloadResponse> PullModelAsync(
        string modelName,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        if (!await IsOllamaReachable())
        {
            SetUnreachableStatus();
            throw NewServiceUnreachableException();
        }

        var payload = new { model = modelName, stream = true };
        var jsonPayload = JsonSerializer.Serialize(payload);
        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        using var request = CreateRequest(HttpMethod.Post, "/api/pull");
        request.Content = content;

        HttpResponseMessage? response;
        try
        {
            // Using check client since this request is sent once and associated with the stream.
            // It doesn't need heavyclient's full timeout, as the API should reply quickly at first.
            response = await _checkHttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        }
        catch (HttpRequestException ex)
        {
            SetUnreachableStatus();
            throw NewServiceUnreachableException(ex);
        }

        if (response.StatusCode == HttpStatusCode.OK)
        {
            Status = new OllamaApiStatus(OllamaApiState.Connected);
        }
        else
        {
            throw new OllamaApiException(response.StatusCode);
        }

        // TODO: Extract stream reading logic into a separate method so proper exceptions will be thrown
        // and they'll be canceled correctly.
        // TODO: Handle the thrown exceptions properly (e.g., catch LostInternetException in HomeViewModel).

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);
        var lastProgressDateTime = DateTime.Now;
        long? previousCompletedValue = 0;

        while (!ct.IsCancellationRequested)
        {
            DownloadResponse? json = null;
            try
            {
                // Check for internet connection if the timeout has passed without progress
                if (DateTime.Now - lastProgressDateTime > _downloadTimeout)
                {
                    if (!await networkManager.IsInternetAvailableAsync())
                    {
                        throw new LostInternetConnectionException();
                    }
                }

                var line = await reader.ReadLineAsync(ct);
                if (line != null)
                {
                    json = JsonSerializer.Deserialize<DownloadResponse>(line, _jsonSerializerOptions);

                    // Check whether the Completed value is received and is increasing
                    if (json is { Completed: not null } && json.Completed > previousCompletedValue)
                    {
                        lastProgressDateTime = DateTime.Now;
                        previousCompletedValue = json.Completed;
                    }
                }
                else break;
            }
            catch (JsonException)
            {
                // TODO: Proper logging
            }

            if (json != null) yield return json;
        }
    }

    public async IAsyncEnumerable<OllamaResponse> GenerateMessageAsync(
        List<Message> messageHistory,
        string modelName,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        // TODO: Pass cancellation token properly when supporting stopping message generation.
        // The default token currently is not cancellable.

        if (!await IsOllamaReachable())
        {
            SetUnreachableStatus();
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
            response = await _heavyHttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                Status = new OllamaApiStatus(OllamaApiState.Connected);
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            SetUnreachableStatus();
            yield break;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);
        while (await reader.ReadLineAsync(ct) is { } line)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            OllamaResponse? json = null;
            try
            {
                json = JsonSerializer.Deserialize<OllamaResponse>(line);
            }
            catch (JsonException)
            {
                // TODO: Proper logging
            }

            if (json != null) yield return json;
        }
    }

    public async Task<bool> DeleteModelAsync(string modelName)
    {
        if (!await IsOllamaReachable())
        {
            SetUnreachableStatus();
            return false;
        }

        var payload = new { model = modelName };
        var jsonPayload = JsonSerializer.Serialize(payload);
        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        using var request = CreateRequest(HttpMethod.Delete, "/api/delete");
        request.Content = content;

        // Using check client since API should not take a long time on this
        using var response = await _checkHttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        return response.StatusCode == HttpStatusCode.OK;
    }

    /// <summary>
    /// Checks if the configured API host points to a remote server.
    /// </summary>
    /// <param name="host">The hostname to check.</param>
    /// <returns>True if the Ollama connection is remote; otherwise, false.</returns>
    public static bool IsConnectionRemote(string host)
    {
        if (string.IsNullOrEmpty(host)) return false;

        return !host.Equals("localhost", StringComparison.OrdinalIgnoreCase) &&
               !host.Equals("127.0.0.1", StringComparison.Ordinal);
    }

    #endregion

    #region Private Helpers

    /// <summary>
    /// Creates the appropriate exception based on whether the connection is local or remote.
    /// </summary>
    /// <returns>A newly created OllamaException to throw.</returns>
    private OllamaException NewServiceUnreachableException(Exception? innerException = null)
    {
        if (IsConnectionRemote(configurationService.ReadSetting(ConfigurationKey.ApiHost)))
        {
            return new OllamaRemoteServerUnreachableException(innerException);
        }

        return new OllamaLocalServerUnreachableException(innerException);
    }

    private void SetUnreachableStatus()
    {
        if (IsConnectionRemote(configurationService.ReadSetting(ConfigurationKey.ApiHost)))
        {
            Status = new OllamaApiStatus(OllamaApiState.Faulted,
                LocalizationService.GetString("OLLAMA_REMOTE_UNREACHABLE"));
        }
        else
        {
            Status = new OllamaApiStatus(OllamaApiState.Faulted,
                LocalizationService.GetString("OLLAMA_LOCAL_UNREACHABLE"));
        }
    }

    /// <summary>
    /// Creates an HttpRequestMessage with the correct base URI derived from the configuration.
    /// </summary>
    private HttpRequestMessage CreateRequest(HttpMethod method, string endpoint)
    {
        var host = configurationService.ReadSetting(ConfigurationKey.ApiHost);
        if (string.IsNullOrEmpty(host) || host == "localhost")
        {
            // Use IPv4 since Windows may resolve 'localhost' to IPv6
            host = "127.0.0.1";
        }

        var port = configurationService.ReadSetting(ConfigurationKey.ApiPort);
        if (string.IsNullOrEmpty(port)) port = "11434";

        var builder = new UriBuilder("http", host, int.Parse(port), endpoint);

        return new HttpRequestMessage(method, builder.Uri);
    }

    private async Task<OllamaTagsResponse?> FetchOllamaTagsAsync()
    {
        try
        {
            using var request = CreateRequest(HttpMethod.Get, "/api/tags");
            using var response = await _checkHttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<OllamaTagsResponse>(json, _jsonSerializerOptions);
        }
        catch (Exception ex) when (ex is HttpRequestException)
        {
            // TODO: Proper logging
            return null;
        }
    }

    private async Task<OllamaShowResponse?> FetchModelInfoAsync(string modelName)
    {
        try
        {
            var payload = new { model = modelName };
            var jsonPayload = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            using var request = CreateRequest(HttpMethod.Post, "/api/show");
            request.Content = content;

            // Using check client since API should not take a long time on this
            using var response = await _checkHttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            var json = await response.Content.ReadAsStringAsync();

            return JsonSerializer.Deserialize<OllamaShowResponse>(json, _jsonSerializerOptions);
        }
        catch (Exception ex) when (ex is HttpRequestException)
        {
            // TODO: Proper logging
            return null;
        }
    }

    private async Task<bool> IsOllamaReachable()
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

    #endregion
}
