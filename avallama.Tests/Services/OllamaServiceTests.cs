// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using avallama.Models;
using avallama.Services;
using avallama.Tests.Fixtures;
using Moq;
using Moq.Protected;
using Xunit;

namespace avallama.Tests.Services;

public class OllamaServiceTests : IClassFixture<TestServicesFixture>
{
    private readonly TestServicesFixture _fixture;
    private readonly Mock<HttpMessageHandler> _handlerMock;

    public OllamaServiceTests(TestServicesFixture fixture)
    {
        _fixture = fixture;

        _fixture.ConfigMock
            .Setup(x => x.ReadSetting(ConfigurationKey.ApiHost))
            .Returns("localhost");

        _fixture.ConfigMock
            .Setup(x => x.ReadSetting(ConfigurationKey.ApiPort))
            .Returns("11434");

        // Create a mock HttpMessageHandler
        _handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);

        // Setup a default SendAsync behavior
        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync((HttpRequestMessage request, CancellationToken _) =>
            {
                if (request.RequestUri!.AbsolutePath == "/api/version")
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent("{\"version\":\"1.0.0\"}")
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.NotFound);
            });
    }

    private Mock<IHttpClientFactory> CreateMockHttpClientFactory(HttpClient httpClient)
    {
        var mockFactory = new Mock<IHttpClientFactory>();

        mockFactory
            .Setup(x => x.CreateClient("OllamaServiceHttpClient"))
            .Returns(httpClient);

        return mockFactory;
    }

    [Fact]
    public async Task StartingOllamaService_WhenMockedRunning_SetsRunning()
    {
        var httpClient = new HttpClient(_handlerMock.Object);
        var mockHttpClientFactory = CreateMockHttpClientFactory(httpClient);

        var ol = new OllamaService(
            _fixture.ConfigMock.Object,
            _fixture.DialogMock.Object,
            _fixture.ModelCacheMock.Object,
            _fixture.ScraperMock.Object,
            new SynchronousAvaloniaDispatcher(),
            mockHttpClientFactory.Object)
        {
            StartProcessFunc = _ => new Process(),
            GetProcessCountFunc = () => 0
        };

        var reportedState = new ServiceState(ServiceStatus.Stopped);
        ol.ServiceStateChanged += status => reportedState = status;
        await ol.Start();

        Assert.Equal(ServiceStatus.Running, reportedState.Status);
    }

    [Fact]
    public async Task StartingOllamaService_WhenProcessIsNull_SetsNotInstalled()
    {
        var httpClient = new HttpClient(_handlerMock.Object);
        var mockHttpClientFactory = CreateMockHttpClientFactory(httpClient);

        var ol = new OllamaService(
            _fixture.ConfigMock.Object,
            _fixture.DialogMock.Object,
            _fixture.ModelCacheMock.Object,
            _fixture.ScraperMock.Object,
            new SynchronousAvaloniaDispatcher(),
            mockHttpClientFactory.Object)
        {
            StartProcessFunc = _ => null,
            GetProcessCountFunc = () => 0
        };

        var reportedState = new ServiceState(ServiceStatus.Stopped);
        ol.ServiceStateChanged += status => reportedState = status;
        await ol.Start();

        Assert.Equal(ServiceStatus.NotInstalled, reportedState.Status);
    }

    [Fact]
    public async Task StartingOllamaService_WithProcessRunning_SetsRunning()
    {
        var httpClient = new HttpClient(_handlerMock.Object);
        var mockHttpClientFactory = CreateMockHttpClientFactory(httpClient);

        var ol = new OllamaService(
            _fixture.ConfigMock.Object,
            _fixture.DialogMock.Object,
            _fixture.ModelCacheMock.Object,
            _fixture.ScraperMock.Object,
            new SynchronousAvaloniaDispatcher(),
            mockHttpClientFactory.Object)
        {
            StartProcessFunc = _ => null,
            GetProcessCountFunc = () => 1
        };

        var reportedState = new ServiceState(ServiceStatus.Stopped);
        ol.ServiceStateChanged += status => reportedState = status;
        await ol.Start();

        Assert.Equal(ServiceStatus.Running, reportedState.Status);
    }

    [Fact]
    public async Task Start_WithDelayedResponse_RetriesUntilRunning()
    {
        var callCount = 0;
        var statusEvents = new List<ServiceStatus?>();

        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .Returns((HttpRequestMessage request, CancellationToken token) =>
            {
                return Task.Run(async () =>
                {
                    if (request.RequestUri!.AbsolutePath != "/api/version")
                        return new HttpResponseMessage(HttpStatusCode.NotFound);

                    Interlocked.Increment(ref callCount);

                    // simulate network delay
                    await Task.Delay(100, token);

                    if (callCount < 4)
                        return new HttpResponseMessage(HttpStatusCode.NotFound);

                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent("{\"version\":\"1.0.0\"}")
                    };
                }, token);
            });

        var httpClient = new HttpClient(_handlerMock.Object);
        var mockHttpClientFactory = CreateMockHttpClientFactory(httpClient);

        var ol = new OllamaService(
            _fixture.ConfigMock.Object,
            _fixture.DialogMock.Object,
            _fixture.ModelCacheMock.Object,
            _fixture.ScraperMock.Object,
            new SynchronousAvaloniaDispatcher(),
            mockHttpClientFactory.Object)
        {
            StartProcessFunc = _ => new Process(),
            GetProcessCountFunc = () => 0,
            RetryDelay = TimeSpan.FromMilliseconds(50)
        };

        ol.ServiceStateChanged += state => { statusEvents.Add(state?.Status); };

        await ol.Start();

        Assert.Equal(1, statusEvents.Count(e => e == ServiceStatus.Running));
        Assert.Equal(1, statusEvents.Count(e => e == ServiceStatus.Retrying));
        Assert.Equal(4, callCount);
    }

    [Fact]
    public async Task Start_WhenServerNeverResponds_StopsAfterMaxRetries()
    {
        var callCount = 0;

        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(() =>
            {
                callCount++;
                throw new HttpRequestException("Connection refused");
            });

        var httpClient = new HttpClient(_handlerMock.Object);
        var mockFactory = CreateMockHttpClientFactory(httpClient);

        var ol = new OllamaService(
            _fixture.ConfigMock.Object,
            _fixture.DialogMock.Object,
            _fixture.ModelCacheMock.Object,
            _fixture.ScraperMock.Object,
            new SynchronousAvaloniaDispatcher(),
            mockFactory.Object)
        {
            StartProcessFunc = _ => new Process(),
            GetProcessCountFunc = () => 0,
            RetryDelay = TimeSpan.FromMilliseconds(1)
        };

        var statusEvents = new List<ServiceStatus>();
        ol.ServiceStateChanged += s => { if (s?.Status != null) statusEvents.Add(s.Status); };

        await ol.Start();

        Assert.Contains(ServiceStatus.Retrying, statusEvents);
        Assert.Equal(ServiceStatus.Stopped, ol.OllamaServiceState?.Status);
        Assert.Equal(6, callCount);
    }

    [Fact]
    public async Task Start_WhenServerIsTooSlow_TreatsAsErrorAndRetries()
    {
        var callCount = 0;

        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .Returns(async (HttpRequestMessage request, CancellationToken token) =>
            {
                callCount++;

                if (callCount == 1)
                {
                    // TaskCanceledException to simulate a timeout
                    await Task.Delay(50, token);
                    throw new TaskCanceledException("Request timed out");
                }

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"version\":\"1.0.0\"}")
                };
            });

        var httpClient = new HttpClient(_handlerMock.Object);
        var mockFactory = CreateMockHttpClientFactory(httpClient);

        var ol = new OllamaService(
            _fixture.ConfigMock.Object,
            _fixture.DialogMock.Object,
            _fixture.ModelCacheMock.Object,
            _fixture.ScraperMock.Object,
            new SynchronousAvaloniaDispatcher(),
            mockFactory.Object)
        {
            StartProcessFunc = _ => new Process(),
            GetProcessCountFunc = () => 0,
            RetryDelay = TimeSpan.FromMilliseconds(10)
        };

        await ol.Start();

        Assert.Equal(2, callCount);
        Assert.Equal(ServiceStatus.Running, ol.OllamaServiceState?.Status);
    }

    [Fact]
    public async Task GenerateMessage_ReturnsResponses_AndSetsRunningStatus()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);

        var msg1 = new MessageContent
        {
            Content = "Hello"
        };
        var msg2 = new MessageContent
        {
            Content = "World!"
        };

        var responses = new[]
        {
            JsonSerializer.Serialize(new OllamaResponse
            {
                Model = "TestLlama", CreatedAt = "2023-08-04T08:52:19.385406455-07:00", Message = msg1, Done = false
            }),
            JsonSerializer.Serialize(new OllamaResponse
            {
                Model = "TestLlama", CreatedAt = "2023-08-04T08:52:19.385406455-07:00", Message = msg2, Done = false
            })
        };

        var responseContent = string.Join("\n", responses);

        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseContent)
            });

        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.RequestUri!.AbsolutePath.Contains("/api/version")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        var httpClient = new HttpClient(mockHandler.Object);
        var mockHttpClientFactory = CreateMockHttpClientFactory(httpClient);

        var ol = new OllamaService(
            _fixture.ConfigMock.Object,
            _fixture.DialogMock.Object,
            _fixture.ModelCacheMock.Object,
            _fixture.ScraperMock.Object,
            new SynchronousAvaloniaDispatcher(),
            mockHttpClientFactory.Object)
        {
            StartProcessFunc = _ => new Process(),
            GetProcessCountFunc = () => 0
        };

        var statusEvents = new List<(ServiceStatus? status, string? message)>();
        ol.ServiceStateChanged += s => statusEvents.Add((s?.Status, s?.Message));

        await ol.Start();

        // Act
        var results = new List<OllamaResponse>();
        await foreach (var r in ol.GenerateMessage(new List<Message>(), "test-model"))
        {
            results.Add(r);
        }

        // Assert
        Assert.Equal(2, results.Count);
        Assert.Equal("Hello", results[0].Message?.Content);
        Assert.Equal("World!", results[1].Message?.Content);

        Assert.Contains(statusEvents, e => e.status == ServiceStatus.Running);
    }

    [Fact]
    public async Task GenerateMessage_InvalidJson_OnlyShowsValid()
    {
        var mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);

        const string responseContent =
            "INVALID_JSON\n{\"model\": \"llama3.2\",\"created_at\": \"2023-08-04T08:52:19.385406455-07:00\",\"message\": {\"role\": \"assistant\", \"content\": \"hello\"},\"done\": false}";

        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseContent)
            });

        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.RequestUri!.AbsolutePath.Contains("/api/version")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        var httpClient = new HttpClient(mockHandler.Object);
        var mockHttpClientFactory = CreateMockHttpClientFactory(httpClient);

        var ol = new OllamaService(
            _fixture.ConfigMock.Object,
            _fixture.DialogMock.Object,
            _fixture.ModelCacheMock.Object,
            _fixture.ScraperMock.Object,
            new SynchronousAvaloniaDispatcher(),
            mockHttpClientFactory.Object)
        {
            StartProcessFunc = _ => new Process(),
            GetProcessCountFunc = () => 0
        };

        await ol.Start();

        var results = new List<OllamaResponse>();
        await foreach (var r in ol.GenerateMessage([], "llama3.2"))
        {
            results.Add(r);
        }

        // Only the valid JSON line should be returned
        Assert.Single(results);
        Assert.Equal("hello", results[0].Message?.Content);
    }

    [Fact]
    public async Task GenerateMessage_WhenConnectionLost_UpdatesStateToStopped()
    {
        var mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);

        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.RequestUri!.AbsolutePath.Contains("/api/version")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.RequestUri!.AbsolutePath.Contains("/api/chat")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ThrowsAsync(new HttpRequestException("Connection reset by peer"));

        var httpClient = new HttpClient(mockHandler.Object);
        var mockFactory = CreateMockHttpClientFactory(httpClient);

        var ol = new OllamaService(
            _fixture.ConfigMock.Object,
            _fixture.DialogMock.Object,
            _fixture.ModelCacheMock.Object,
            _fixture.ScraperMock.Object,
            new SynchronousAvaloniaDispatcher(),
            mockFactory.Object)
        {
            StartProcessFunc = _ => new Process(),
            GetProcessCountFunc = () => 0
        };

        var statusEvents = new List<ServiceStatus>();
        ol.ServiceStateChanged += s => { if (s?.Status != null) statusEvents.Add(s.Status); };

        await ol.Start();

        await foreach (var _ in ol.GenerateMessage([], "model")) { }

        Assert.Equal(ServiceStatus.Stopped, ol.OllamaServiceState?.Status);
        Assert.Contains(ServiceStatus.Stopped, statusEvents);
    }
}
