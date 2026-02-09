// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Concurrent;
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
using avallama.Extensions;
using avallama.Models;
using avallama.Models.Dtos;
using avallama.Models.Ollama;
using avallama.Services.Persistence;
using avallama.Utilities.Time;

namespace avallama.Services.Ollama;

public delegate void OllamaApiStatusChangedHandler(OllamaApiStatus status);

public interface IOllamaApiClient
{
    OllamaApiStatus Status { get; }
    event OllamaApiStatusChangedHandler? StatusChanged;
    Task CheckConnectionAsync();
    Task RetryConnectionAsync();
    Task<IList<OllamaModel>> GetDownloadedModelsAsync();
    Task UpdateDownloadedModelsAsync();
    Task<bool> DeleteModelAsync(string modelName);

    IAsyncEnumerable<DownloadResponse> PullModelAsync(
        string modelName,
        CancellationToken ct = default
    );

    IAsyncEnumerable<OllamaResponse> GenerateMessageAsync(
        List<Message> messageHistory,
        string modelName,
        CancellationToken ct = default);
}

public class OllamaApiClient : IOllamaApiClient
{
    // Default server configuration
    public const int DefaultApiPort = 11434;
    public const string DefaultApiHost = "localhost";

    // Dependencies
    private readonly IConfigurationService _configurationService;
    private readonly IDialogService _dialogService;
    private readonly IModelCacheService _modelCacheService;
    private readonly HttpClient _checkHttpClient;
    private readonly HttpClient _heavyHttpClient;
    private readonly ITimeProvider _timeProvider;
    private readonly ITaskDelayer _taskDelayer;

    public TimeSpan DownloadTimeout { get; init; } = TimeSpan.FromSeconds(5);
    public TimeSpan MaxRetryingTime { get; init; } = TimeSpan.FromSeconds(15);
    public TimeSpan ConnectionCheckInterval { get; init; } = TimeSpan.FromMilliseconds(500);

    private List<OllamaModel>? _downloadedModels;
    private readonly JsonSerializerOptions _jsonSerializerOptions = new() { PropertyNameCaseInsensitive = true };

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

    public OllamaApiClient(
        IConfigurationService configurationService,
        IDialogService dialogService,
        IModelCacheService modelCacheService,
        IHttpClientFactory httpClientFactory,
        ITimeProvider? timeProvider = null,
        ITaskDelayer? taskDelayer = null)
    {
        _configurationService = configurationService;
        _dialogService = dialogService;
        _modelCacheService = modelCacheService;

        _checkHttpClient = httpClientFactory.CreateClient("OllamaCheckHttpClient");
        _heavyHttpClient = httpClientFactory.CreateClient("OllamaHeavyHttpClient");

        _timeProvider = timeProvider ?? new RealTimeProvider();
        _taskDelayer = taskDelayer ?? new RealTaskDelayer();
    }

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
            return new List<OllamaModel>();
        }

        if (_downloadedModels == null || _downloadedModels.Count == 0)
        {
            await UpdateDownloadedModelsAsync();
        }

        return _downloadedModels ?? [];
    }

    public async Task UpdateDownloadedModelsAsync()
    {
        if (!await IsOllamaReachable())
        {
            SetUnreachableStatus();
            return;
        }

        var tagsResponse = await FetchOllamaTagsAsync();
        if (tagsResponse?.Models == null) return;

        var downloadedModels = new ConcurrentBag<OllamaModel>();

        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = 5
        };

        await Parallel.ForEachAsync(tagsResponse.Models.Where(m => !string.IsNullOrEmpty(m.Name)), parallelOptions,
            async (ollamaModelDto, _) =>
            {
                var downloadedModel = ollamaModelDto.ConvertToOllamaModel();
                try
                {
                    var showResponse = await FetchModelInfoAsync(downloadedModel.Name);
                    downloadedModel.EnrichWith(showResponse);
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
            response = await _heavyHttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
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

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);

        var line = await reader.ReadLineAsync(ct);
        while (line != null)
        {
            DownloadResponse? json = null;
            try
            {
                line = await reader.ReadLineAsync(ct);
                Console.WriteLine(line);
                if (line == null) break;
                json = JsonSerializer.Deserialize<DownloadResponse>(line, _jsonSerializerOptions);
            }
            catch (JsonException)
            {
                // TODO: proper logging
            }

            if (json != null) yield return json;
        }
    }

    public async IAsyncEnumerable<OllamaResponse> GenerateMessageAsync(
        List<Message> messageHistory,
        string modelName,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        // TODO: pass cancellation token properly when we'll support stopping the message generation

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
            catch (JsonException e)
            {
                _dialogService.ShowErrorDialog(e.Message, false);
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

        using var response = await _heavyHttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        return response.StatusCode == HttpStatusCode.OK;
    }

    /// <summary>
    /// Checks if the configured API host points to a remote server.
    /// </summary>
    /// <returns>True if the Ollama connection is remote; otherwise, false.</returns>
    public static bool IsConnectionRemote(string host)
    {
        if (string.IsNullOrEmpty(host)) return false;

        return !host.Equals("localhost", StringComparison.OrdinalIgnoreCase) &&
               !host.Equals("127.0.0.1", StringComparison.Ordinal);
    }

    /// <summary>
    /// Creates the appropriate exception based on whether the connection is local or remote.
    /// <returns>Newly created exception</returns>
    /// </summary>
    private OllamaException NewServiceUnreachableException(Exception? innerException = null)
    {
        if (IsConnectionRemote(_configurationService.ReadSetting(ConfigurationKey.ApiHost)))
        {
            return new OllamaRemoteServerUnreachableException(innerException);
        }

        return new OllamaLocalServerUnreachableException(innerException);
    }

    private void SetUnreachableStatus()
    {
        if (IsConnectionRemote(_configurationService.ReadSetting(ConfigurationKey.ApiHost)))
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
}
