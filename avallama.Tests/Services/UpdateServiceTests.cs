using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Moq.Protected;
using avallama.Services;
using avallama.Utilities.Network;
using Xunit;

namespace avallama.Tests.Services;

public class UpdateServiceTests
{
    private readonly Mock<INetworkManager> _networkManagerMock;
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly HttpClient _httpClient;

    public UpdateServiceTests()
    {
        _networkManagerMock = new Mock<INetworkManager>();
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpMessageHandlerMock.Object);
    }

    private UpdateService CreateService() => new(_httpClient, _networkManagerMock.Object);

    private void SetupHttpResponse(HttpStatusCode statusCode, string content)
    {
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(content)
            });
    }

    [Fact]
    public async Task IsUpdateAvailableAsync_NoInternet_ReturnsFalse()
    {
        _networkManagerMock.Setup(x => x.IsInternetAvailableAsync()).ReturnsAsync(false);
        var service = CreateService();

        var result = await service.IsUpdateAvailableAsync();

        Assert.False(result);
    }

    [Fact]
    public async Task IsUpdateAvailableAsync_HttpRequestFails_ReturnsFalse()
    {
        _networkManagerMock.Setup(x => x.IsInternetAvailableAsync()).ReturnsAsync(true);
        SetupHttpResponse(HttpStatusCode.InternalServerError, "");
        var service = CreateService();

        var result = await service.IsUpdateAvailableAsync();

        Assert.False(result);
    }

    [Fact]
    public async Task IsUpdateAvailableAsync_NewVersionAvailable_ReturnsTrue()
    {
        _networkManagerMock.Setup(x => x.IsInternetAvailableAsync()).ReturnsAsync(true);
        var json = JsonSerializer.Serialize(new { tag_name = "v99.0.0" });
        SetupHttpResponse(HttpStatusCode.OK, json);
        var service = CreateService();

        var result = await service.IsUpdateAvailableAsync();

        Assert.True(result);
    }

    [Fact]
    public async Task IsUpdateAvailableAsync_SameVersion_ReturnsFalse()
    {
        _networkManagerMock.Setup(x => x.IsInternetAvailableAsync()).ReturnsAsync(true);
        const string currentVersion = App.Version;
        var json = JsonSerializer.Serialize(new { tag_name = currentVersion });
        SetupHttpResponse(HttpStatusCode.OK, json);
        var service = CreateService();

        var result = await service.IsUpdateAvailableAsync();

        Assert.False(result);
    }

    [Fact]
    public async Task IsUpdateAvailableAsync_CallsCorrectGitHubEndpoint()
    {
        _networkManagerMock.Setup(x => x.IsInternetAvailableAsync()).ReturnsAsync(true);
        var json = JsonSerializer.Serialize(new { tag_name = "v1.0.0" });
        SetupHttpResponse(HttpStatusCode.OK, json);
        var service = CreateService();

        await service.IsUpdateAvailableAsync();

        _httpMessageHandlerMock
            .Protected()
            .Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.RequestUri!.ToString() == "https://api.github.com/repos/4foureyes/avallama/releases/latest"),
                ItExpr.IsAny<CancellationToken>());
    }
}
