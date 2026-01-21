using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using avallama.Utilities.Network;
using Xunit;

namespace avallama.Tests.Utilities.Network;

public sealed class NetworkManagerTests
{
    [Fact]
    public async Task IsInternetAvailableAsync_ReturnsTrue_WhenHeadReturnsSuccessStatusCode()
    {
        var handler = new RecordingHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));

        var nm = new NetworkManager(new TestHttpClientFactory(new HttpClient(handler)));

        var result = await nm.IsInternetAvailableAsync();

        Assert.True(result);
    }

    [Fact]
    public async Task IsInternetAvailableAsync_ReturnsFalse_WhenHeadReturnsNonSuccessStatusCode()
    {
        var handler = new RecordingHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)));

        var nm = new NetworkManager(new TestHttpClientFactory(new HttpClient(handler)));

        var result = await nm.IsInternetAvailableAsync();

        Assert.False(result);
    }

    [Fact]
    public async Task IsInternetAvailableAsync_ReturnsFalse_WhenHttpRequestThrowsHttpRequestException()
    {
        var handler = new RecordingHandler((_, _) =>
            throw new HttpRequestException("network error"));

        var nm = new NetworkManager(new TestHttpClientFactory(new HttpClient(handler)));

        var result = await nm.IsInternetAvailableAsync();

        Assert.False(result);
    }

    [Fact]
    public async Task IsInternetAvailableAsync_ReturnsFalse_WhenHttpClientTimesOut()
    {
        var handler = new RecordingHandler(async (_, ct) =>
        {
            await Task.Delay(TimeSpan.FromSeconds(5), ct);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        using var client = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromMilliseconds(50)
        };

        var nm = new NetworkManager(new TestHttpClientFactory(client));

        var result = await nm.IsInternetAvailableAsync();

        Assert.False(result);
    }

    [Fact]
    public async Task IsInternetAvailableAsync_UsesHeadMethod_AndExpectedUrl()
    {
        var handler = new RecordingHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));

        var nm = new NetworkManager(new TestHttpClientFactory(new HttpClient(handler)));

        _ = await nm.IsInternetAvailableAsync();

        Assert.NotNull(handler.LastRequest);
        Assert.Equal(HttpMethod.Head, handler.LastRequest!.Method);
        Assert.Equal(new Uri("https://1.1.1.1"), handler.LastRequest.RequestUri);
    }

    private sealed class TestHttpClientFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private sealed class RecordingHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> sendAsync)
        : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return sendAsync(request, cancellationToken);
        }
    }
}
