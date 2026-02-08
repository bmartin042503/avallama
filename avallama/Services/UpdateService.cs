using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

using avallama.Utilities.Network;

namespace avallama.Services;

/// <summary>
/// Service responsible for checking if a new version of the application is available
/// </summary>
public interface IUpdateService
{
    /// <summary>
    /// Checks if a new version of the application is available by querying the GitHub API for the latest release
    /// and comparing it to the current version, which is stored in the "VERSION" localization string.
    /// </summary>
    /// <returns> True if an update is available, false otherwise </returns>
    Task<bool> IsUpdateAvailableAsync();
}

public class UpdateService : IUpdateService
{
    private readonly string _version;
    private readonly HttpClient _httpClient;
    private readonly INetworkManager _networkManager;

    public UpdateService(HttpClient httpClient, INetworkManager networkManager)
    {
        _version = App.Version;
        _httpClient = httpClient;
        _httpClient.DefaultRequestHeaders.UserAgent.Add(
            new ProductInfoHeaderValue("avallama", _version));
        _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        _networkManager = networkManager;
    }

    /// <inheritdoc/>
    public async Task<bool> IsUpdateAvailableAsync()
    {
        // If no internet is available, assume no update is available
        if (!await _networkManager.IsInternetAvailableAsync()) return false;

        var response = await _httpClient.GetAsync(
            "https://api.github.com/repos/4foureyes/avallama/releases/latest");

        // If request fails, assume no update is available
        if (!response.IsSuccessStatusCode) return false;

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var latestVersion = doc.RootElement.GetProperty("tag_name").GetString();
        return latestVersion != _version;
    }
}
