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

/// <summary>
/// Defines a high-level service for interacting with the Ollama ecosystem.
/// This acts as a facade, aggregating process management, API communication, and web scraping capabilities.
/// </summary>
public interface IOllamaService
{
    #region Interface

    /// <summary>
    /// Gets the current status of the local Ollama process.
    /// </summary>
    OllamaProcessStatus CurrentProcessStatus { get; }

    /// <summary>
    /// Gets the current status of the Ollama API connection.
    /// </summary>
    OllamaApiStatus CurrentApiStatus { get; }

    /// <summary>
    /// Event raised when the status of the local Ollama process changes.
    /// </summary>
    event OllamaProcessStatusChangedHandler? ProcessStatusChanged;

    /// <summary>
    /// Event raised when the status of the Ollama API connection changes.
    /// </summary>
    event OllamaApiStatusChangedHandler? ApiStatusChanged;

    /// <summary>
    /// Starts the local Ollama process asynchronously.
    /// </summary>
    Task StartOllamaProcessAsync();

    /// <summary>
    /// Stops the local Ollama process asynchronously.
    /// </summary>
    Task StopOllamaProcessAsync();

    /// <summary>
    /// Manually triggers a check for the Ollama API connection status.
    /// </summary>
    Task CheckOllamaApiConnectionAsync();

    /// <summary>
    /// Attempts to reconnect to the Ollama API.
    /// </summary>
    Task RetryOllamaApiConnectionAsync();

    /// <summary>
    /// Retrieves a list of models currently downloaded via the API.
    /// </summary>
    /// <returns>A list of downloaded Ollama models.</returns>
    Task<IList<OllamaModel>> GetDownloadedModelsAsync();

    /// <summary>
    /// Deletes a specified model from the library via the API.
    /// </summary>
    /// <param name="modelName">The name of the model to delete.</param>
    /// <returns>True if deletion was successful; otherwise, false.</returns>
    Task<bool> DeleteModelAsync(string modelName);

    /// <summary>
    /// Enriches a model object with detailed metadata from the API (/api/tags, /api/show).
    /// </summary>
    /// <param name="model">The model to enrich.</param>
    Task EnrichModelAsync(OllamaModel model);

    /// <summary>
    /// Downloads (pulls) a specific model from the Ollama library.
    /// </summary>
    /// <param name="modelName">The name of the model to download.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>An async stream of download progress updates.</returns>
    IAsyncEnumerable<DownloadResponse> DownloadModelAsync(string modelName, CancellationToken ct = default);

    /// <summary>
    /// Generates a response from a model based on a conversation history.
    /// </summary>
    /// <param name="messageHistory">The list of previous messages in the conversation.</param>
    /// <param name="modelName">The model to use for generation.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>An async stream of response chunks.</returns>
    IAsyncEnumerable<OllamaResponse> GenerateMessageAsync(List<Message> messageHistory, string modelName, CancellationToken ct = default);

    /// <summary>
    /// Retrieves model families fetched during the last scraping session.
    /// </summary>
    /// <returns>A list of model families.</returns>
    Task<IList<OllamaModelFamily>> GetScrapedFamiliesAsync();

    /// <summary>
    /// Scrapes and streams all available models from the Ollama online library.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An async stream of discovered models.</returns>
    IAsyncEnumerable<OllamaModel> StreamAllScrapedModelsAsync(CancellationToken cancellationToken);

    #endregion
}

/// <summary>
/// Implementation of the IOllamaService facade, orchestrating process management, API calls, and scraping.
/// </summary>
public class OllamaService(
    IOllamaProcessManager processManager,
    IOllamaApiClient apiClient,
    IOllamaScraper ollamaScraper)
    : IOllamaService
{
    private OllamaScraperResult? _currentScrapeSession;

    #region Events & Status

    public event OllamaProcessStatusChangedHandler? ProcessStatusChanged
    {
        add => processManager.StatusChanged += value;
        remove => processManager.StatusChanged -= value;
    }

    public event OllamaApiStatusChangedHandler? ApiStatusChanged
    {
        add => apiClient.StatusChanged += value;
        remove => apiClient.StatusChanged -= value;
    }

    public OllamaApiStatus CurrentApiStatus => apiClient.Status;

    public OllamaProcessStatus CurrentProcessStatus => processManager.Status;

    #endregion

    #region Process Management

    public async Task StartOllamaProcessAsync() => await processManager.StartAsync();
    public async Task StopOllamaProcessAsync() => await processManager.StopAsync();

    #endregion

    #region API

    public async Task CheckOllamaApiConnectionAsync() => await apiClient.CheckConnectionAsync();
    public async Task RetryOllamaApiConnectionAsync() => await apiClient.RetryConnectionAsync();
    public async Task EnrichModelAsync(OllamaModel model) => await apiClient.EnrichModelAsync(model);
    public async Task<IList<OllamaModel>> GetDownloadedModelsAsync() => await apiClient.GetDownloadedModelsAsync();
    public async Task<bool> DeleteModelAsync(string modelName) => await apiClient.DeleteModelAsync(modelName);

    public async IAsyncEnumerable<DownloadResponse> DownloadModelAsync(
        string modelName,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var response in apiClient.PullModelAsync(modelName, ct))
        {
            yield return response;
        }
    }

    public async IAsyncEnumerable<OllamaResponse> GenerateMessageAsync(
        List<Message> messageHistory,
        string modelName,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var response in apiClient.GenerateMessageAsync(messageHistory, modelName, ct))
        {
            yield return response;
        }
    }

    #endregion

    #region Scraper

    public Task<IList<OllamaModelFamily>> GetScrapedFamiliesAsync()
    {
        // Return families from the cached scrape session if available, then clear the session cache.
        if (_currentScrapeSession?.Families is not { } families)
            return Task.FromResult<IList<OllamaModelFamily>>([]);

        _currentScrapeSession = null;
        return Task.FromResult(families);
    }

    public async IAsyncEnumerable<OllamaModel> StreamAllScrapedModelsAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        _currentScrapeSession = null;
        var result = await ollamaScraper.GetAllOllamaModelsAsync(cancellationToken);
        _currentScrapeSession = result;

        await foreach (var model in result.Models.WithCancellation(cancellationToken))
        {
            yield return model;
        }
    }

    #endregion
}
