// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using avallama.Models;
using avallama.Models.Dtos;
using avallama.Models.Ollama;

namespace avallama.Services.Ollama;

public interface IOllamaService
{
    OllamaProcessStatus CurrentProcessStatus { get; }
    OllamaApiStatus CurrentApiStatus { get; }
    event OllamaProcessStatusChangedHandler? ProcessStatusChanged;
    event OllamaApiStatusChangedHandler? ApiStatusChanged;
    Task StartOllamaProcessAsync();
    Task CheckOllamaApiConnectionAsync();
    Task StopOllamaProcessAsync();
    Task RetryOllamaApiConnectionAsync();
    IAsyncEnumerable<DownloadResponse> DownloadModelAsync(string modelName, CancellationToken ct = default);
    IAsyncEnumerable<OllamaResponse> GenerateMessageAsync(List<Message> messageHistory, string modelName, CancellationToken ct = default);
    Task<bool> DeleteModelAsync(string modelName);
    Task<IList<OllamaModel>> GetDownloadedModelsAsync();
    Task UpdateDownloadedModelsAsync();
    Task<IList<OllamaModelFamily>> GetScrapedFamiliesAsync();
    IAsyncEnumerable<OllamaModel> StreamAllScrapedModelsAsync(CancellationToken cancellationToken);
}

public class OllamaService : IOllamaService
{
    private readonly IOllamaProcessManager _processManager;
    private readonly IOllamaApiClient _apiClient;
    private readonly IOllamaScraper _ollamaScraper;

    private OllamaScraperResult? _currentScrapeSession;

    public event OllamaProcessStatusChangedHandler? ProcessStatusChanged
    {
        add => _processManager.StatusChanged += value;
        remove => _processManager.StatusChanged -= value;
    }

    public event OllamaApiStatusChangedHandler? ApiStatusChanged
    {
        add => _apiClient.StatusChanged += value;
        remove => _apiClient.StatusChanged -= value;
    }

    public OllamaApiStatus CurrentApiStatus => _apiClient.Status;
    public OllamaProcessStatus CurrentProcessStatus => _processManager.Status;

    public OllamaService(
        IOllamaProcessManager processManager,
        IOllamaApiClient apiClient,
        IOllamaScraper ollamaScraper)
    {
        _processManager = processManager;
        _apiClient = apiClient;
        _ollamaScraper = ollamaScraper;
    }

    public async Task StartOllamaProcessAsync()
    {
        await _processManager.StartAsync();
    }

    public async Task StopOllamaProcessAsync()
    {
        await _processManager.StopAsync();
    }

    public async Task CheckOllamaApiConnectionAsync()
    {
        await _apiClient.CheckConnectionAsync();
    }

    public async Task RetryOllamaApiConnectionAsync()
    {
        await _apiClient.RetryConnectionAsync();
    }

    public async Task<IList<OllamaModel>> GetDownloadedModelsAsync()
    {
        return await _apiClient.GetDownloadedModelsAsync();
    }

    public async Task UpdateDownloadedModelsAsync()
    {
        await _apiClient.GetDownloadedModelsAsync();
    }

    public async IAsyncEnumerable<DownloadResponse> DownloadModelAsync(
        string modelName,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var response in _apiClient.PullModelAsync(modelName, ct))
        {
            yield return response;
        }
    }

    public async Task<bool> DeleteModelAsync(string modelName)
    {
        return await _apiClient.DeleteModelAsync(modelName);
    }

    public async IAsyncEnumerable<OllamaResponse> GenerateMessageAsync(
        List<Message> messageHistory,
        string modelName,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var response in _apiClient.GenerateMessageAsync(messageHistory, modelName, ct))
        {
            yield return response;
        }
    }

    public Task<IList<OllamaModelFamily>> GetScrapedFamiliesAsync()
    {
        if (_currentScrapeSession?.Families is not { } families)
            return Task.FromResult<IList<OllamaModelFamily>>([]);

        _currentScrapeSession = null;
        return Task.FromResult(families);
    }

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
}
