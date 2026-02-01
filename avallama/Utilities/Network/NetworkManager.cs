// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;

namespace avallama.Utilities.Network;

/// <summary>
/// Defines the interface for network management functionalities.
/// </summary>
public interface INetworkManager
{
    /// <summary>
    /// Checks if the internet is available by getting the header for 1.1.1.1.
    /// <returns> True if internet is available (Header arrives in less than 1 second), False otherwise </returns>
    /// </summary>
    public Task<bool> IsInternetAvailableAsync();
}

/// <summary>
/// Implements network management functionalities.
/// </summary>
public class NetworkManager(IHttpClientFactory httpClientFactory) : INetworkManager
{
    /// <summary>
    /// HttpClient using the "OllamaCheckHttpClient" configuration.
    /// </summary>
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("OllamaCheckHttpClient");

    /// <inheritdoc/>
    public async Task<bool> IsInternetAvailableAsync()
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Head, "https://1.1.1.1");
            using var response = await _httpClient
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead)
                .ConfigureAwait(false);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex) when (
            ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            return false;
        }
    }
}
