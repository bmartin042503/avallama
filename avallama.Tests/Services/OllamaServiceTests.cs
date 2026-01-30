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
using avallama.Constants;
using avallama.Models;
using avallama.Models.Dtos;
using avallama.Services.Ollama;
using avallama.Services.Persistence;
using avallama.Tests.Mocks;
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

    private Mock<IHttpClientFactory> CreateMockHttpClientFactory(HttpClient checkClient, HttpClient heavyClient)
    {
        var mockFactory = new Mock<IHttpClientFactory>();

        mockFactory
            .Setup(x => x.CreateClient("OllamaCheckHttpClient"))
            .Returns(checkClient);

        mockFactory
            .Setup(x => x.CreateClient("OllamaHeavyHttpClient"))
            .Returns(heavyClient);

        return mockFactory;
    }

    [Fact]
    public async Task StartingOllamaService_SetsRunningState()
    {
        var timeMock = new TimeProviderMock();
        var delayerMock = new TaskDelayerMock(timeMock);

        var httpClient = new HttpClient(_handlerMock.Object);
        var mockHttpClientFactory = CreateMockHttpClientFactory(httpClient, httpClient);

        var ol = new OllamaService(
            _fixture.ConfigMock.Object,
            _fixture.DialogMock.Object,
            _fixture.ModelCacheMock.Object,
            _fixture.ScraperMock.Object,
            _fixture.NetworkManagerMock.Object,
            new SynchronousAvaloniaDispatcher(),
            mockHttpClientFactory.Object,
            timeMock,
            delayerMock)
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
    public async Task StartingOllamaService_WhenProcessIsNull_SetsNotInstalledState()
    {
        var timeMock = new TimeProviderMock();
        var delayerMock = new TaskDelayerMock(timeMock);

        var httpClient = new HttpClient(_handlerMock.Object);
        var mockHttpClientFactory = CreateMockHttpClientFactory(httpClient, httpClient);

        var ol = new OllamaService(
            _fixture.ConfigMock.Object,
            _fixture.DialogMock.Object,
            _fixture.ModelCacheMock.Object,
            _fixture.ScraperMock.Object,
            _fixture.NetworkManagerMock.Object,
            new SynchronousAvaloniaDispatcher(),
            mockHttpClientFactory.Object,
            timeMock,
            delayerMock)
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
    public async Task StartingOllamaService_WithProcessRunning_SetsRunningState()
    {
        var timeMock = new TimeProviderMock();
        var delayerMock = new TaskDelayerMock(timeMock);

        var httpClient = new HttpClient(_handlerMock.Object);
        var mockHttpClientFactory = CreateMockHttpClientFactory(httpClient, httpClient);

        var ol = new OllamaService(
            _fixture.ConfigMock.Object,
            _fixture.DialogMock.Object,
            _fixture.ModelCacheMock.Object,
            _fixture.ScraperMock.Object,
            _fixture.NetworkManagerMock.Object,
            new SynchronousAvaloniaDispatcher(),
            mockHttpClientFactory.Object,
            timeMock,
            delayerMock)
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
        var timeMock = new TimeProviderMock();
        var delayerMock = new TaskDelayerMock(timeMock);

        var callCount = 0;
        var statusEvents = new List<ServiceStatus?>();

        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync((HttpRequestMessage request, CancellationToken token) =>
            {
                if (request.RequestUri!.AbsolutePath != "/api/version")
                    return new HttpResponseMessage(HttpStatusCode.NotFound);

                callCount++; // doesn't need interlocked because it runs synchronously with fake time

                if (callCount < 4)
                    return new HttpResponseMessage(HttpStatusCode.NotFound);

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"version\":\"1.0.0\"}")
                };
            });

        var httpClient = new HttpClient(_handlerMock.Object);
        var mockHttpClientFactory = CreateMockHttpClientFactory(httpClient, httpClient);

        var ol = new OllamaService(
            _fixture.ConfigMock.Object,
            _fixture.DialogMock.Object,
            _fixture.ModelCacheMock.Object,
            _fixture.ScraperMock.Object,
            _fixture.NetworkManagerMock.Object,
            new SynchronousAvaloniaDispatcher(),
            mockHttpClientFactory.Object,
            timeMock,
            delayerMock)
        {
            StartProcessFunc = _ => new Process(),
            GetProcessCountFunc = () => 0,
            MaxRetryingTime = TimeSpan.FromSeconds(2),
            ConnectionCheckInterval = TimeSpan.FromMilliseconds(50)
        };

        ol.ServiceStateChanged += state => { statusEvents.Add(state?.Status); };

        await ol.Start();

        Assert.Equal(1, statusEvents.Count(e => e == ServiceStatus.Running));
        Assert.Equal(1, statusEvents.Count(e => e == ServiceStatus.Retrying));
        Assert.Equal(4, callCount);
        Assert.True(timeMock.Elapsed.TotalMilliseconds >= 100);
    }

    [Fact]
    public async Task Start_WhenServerNeverResponds_FailsAfterMaxRetries()
    {
        var timeMock = new TimeProviderMock();
        var delayerMock = new TaskDelayerMock(timeMock);

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
        var mockHttpClientFactory = CreateMockHttpClientFactory(httpClient, httpClient);

        var ol = new OllamaService(
            _fixture.ConfigMock.Object,
            _fixture.DialogMock.Object,
            _fixture.ModelCacheMock.Object,
            _fixture.ScraperMock.Object,
            _fixture.NetworkManagerMock.Object,
            new SynchronousAvaloniaDispatcher(),
            mockHttpClientFactory.Object,
            timeMock,
            delayerMock)
        {
            StartProcessFunc = _ => new Process(),
            GetProcessCountFunc = () => 0,
            MaxRetryingTime = TimeSpan.FromSeconds(2),
            ConnectionCheckInterval = TimeSpan.FromMilliseconds(50)
        };

        var statusEvents = new List<ServiceStatus>();
        ol.ServiceStateChanged += s =>
        {
            if (s?.Status != null) statusEvents.Add(s.Status);
        };

        await ol.Start();

        Assert.Equal(ServiceStatus.Retrying, statusEvents[0]);
        Assert.Equal(ServiceStatus.Failed, statusEvents[1]);
        Assert.Equal(2, statusEvents.Count);
        Assert.Equal(ServiceStatus.Failed, ol.OllamaServiceState?.Status);
        Assert.True(callCount > 1, $"Expected multiple calls, but got {callCount}");
    }

    [Fact]
    public async Task Start_WhenServerIsTooSlow_TreatsAsErrorAndRetries()
    {
        var callCount = 0;

        var timeMock = new TimeProviderMock();
        var delayerMock = new TaskDelayerMock(timeMock);

        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .Returns((HttpRequestMessage _, CancellationToken token) =>
            {
                callCount++;

                if (callCount == 1)
                {
                    timeMock.Advance(TimeSpan.FromMilliseconds(50));

                    // simulating a timeout
                    throw new TaskCanceledException("Request timed out");
                }

                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"version\":\"1.0.0\"}")
                };

                return Task.FromResult(response);
            });

        var httpClient = new HttpClient(_handlerMock.Object);
        var mockHttpClientFactory = CreateMockHttpClientFactory(httpClient, httpClient);

        var ol = new OllamaService(
            _fixture.ConfigMock.Object,
            _fixture.DialogMock.Object,
            _fixture.ModelCacheMock.Object,
            _fixture.ScraperMock.Object,
            _fixture.NetworkManagerMock.Object,
            new SynchronousAvaloniaDispatcher(),
            mockHttpClientFactory.Object,
            timeMock,
            delayerMock)
        {
            StartProcessFunc = _ => new Process(),
            GetProcessCountFunc = () => 0
        };

        var statusEvents = new List<ServiceStatus>();
        ol.ServiceStateChanged += s =>
        {
            if (s?.Status != null) statusEvents.Add(s.Status);
        };

        await ol.Start();

        Assert.Equal(2, callCount);
        Assert.Equal(ServiceStatus.Retrying, statusEvents[0]);
        Assert.Equal(ServiceStatus.Running, ol.OllamaServiceState?.Status);
    }

    [Fact]
    public async Task GenerateMessage_ReturnsResponses_AndSetsRunningStatus()
    {
        var timeMock = new TimeProviderMock();
        var delayerMock = new TaskDelayerMock(timeMock);

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
        var mockHttpClientFactory = CreateMockHttpClientFactory(httpClient, httpClient);

        var ol = new OllamaService(
            _fixture.ConfigMock.Object,
            _fixture.DialogMock.Object,
            _fixture.ModelCacheMock.Object,
            _fixture.ScraperMock.Object,
            _fixture.NetworkManagerMock.Object,
            new SynchronousAvaloniaDispatcher(),
            mockHttpClientFactory.Object,
            timeMock,
            delayerMock)
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
        var timeMock = new TimeProviderMock();
        var delayerMock = new TaskDelayerMock(timeMock);

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
        var mockHttpClientFactory = CreateMockHttpClientFactory(httpClient, httpClient);

        var ol = new OllamaService(
            _fixture.ConfigMock.Object,
            _fixture.DialogMock.Object,
            _fixture.ModelCacheMock.Object,
            _fixture.ScraperMock.Object,
            _fixture.NetworkManagerMock.Object,
            new SynchronousAvaloniaDispatcher(),
            mockHttpClientFactory.Object,
            timeMock,
            delayerMock)
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
    public async Task GenerateMessage_WhenConnectionLost_UpdatesStateToFailed()
    {
        var timeMock = new TimeProviderMock();
        var delayerMock = new TaskDelayerMock(timeMock);

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
        var mockHttpClientFactory = CreateMockHttpClientFactory(httpClient, httpClient);

        var ol = new OllamaService(
            _fixture.ConfigMock.Object,
            _fixture.DialogMock.Object,
            _fixture.ModelCacheMock.Object,
            _fixture.ScraperMock.Object,
            _fixture.NetworkManagerMock.Object,
            new SynchronousAvaloniaDispatcher(),
            mockHttpClientFactory.Object,
            timeMock,
            delayerMock)
        {
            StartProcessFunc = _ => new Process(),
            GetProcessCountFunc = () => 0
        };

        var statusEvents = new List<ServiceStatus>();
        ol.ServiceStateChanged += s =>
        {
            if (s?.Status != null) statusEvents.Add(s.Status);
        };

        await ol.Start();

        await foreach (var _ in ol.GenerateMessage([], "model"))
        {
        }

        Assert.Equal(ServiceStatus.Failed, ol.OllamaServiceState?.Status);
        Assert.Contains(ServiceStatus.Failed, statusEvents);
    }

    [Fact]
    public async Task GenerateMessage_WhenModelLoadIsSlow_ShouldNotTimeout()
    {
        var timeMock = new TimeProviderMock();
        var delayerMock = new TaskDelayerMock(timeMock);

        var mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        var msg = new MessageContent { Content = "Finally loaded!" };
        var responseJson = JsonSerializer.Serialize(new OllamaResponse
        {
            Model = "HeavyModel",
            Message = msg,
            Done = true
        });

        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.RequestUri!.AbsolutePath.Contains("/api/version")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        // slow chat response
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.RequestUri!.AbsolutePath.Contains("/api/chat")),
                ItExpr.IsAny<CancellationToken>()
            )
            .Returns((HttpRequestMessage _, CancellationToken token) =>
            {
                // simulating a delay
                timeMock.Advance(TimeSpan.FromMilliseconds(200));

                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(responseJson)
                };

                return Task.FromResult(response);
            });

        // Create CheckClient with short timeout (simulating fast ping requirement)
        var checkClient = new HttpClient(mockHandler.Object);
        checkClient.Timeout = TimeSpan.FromMilliseconds(50);

        // Create HeavyClient with long timeout (simulating patience for model loading)
        var heavyClient = new HttpClient(mockHandler.Object);
        heavyClient.Timeout = TimeSpan.FromMilliseconds(500);

        var mockHttpClientFactory = CreateMockHttpClientFactory(checkClient, heavyClient);

        var ol = new OllamaService(
            _fixture.ConfigMock.Object,
            _fixture.DialogMock.Object,
            _fixture.ModelCacheMock.Object,
            _fixture.ScraperMock.Object,
            _fixture.NetworkManagerMock.Object,
            new SynchronousAvaloniaDispatcher(),
            mockHttpClientFactory.Object,
            timeMock,
            delayerMock)
        {
            StartProcessFunc = _ => new Process(),
            GetProcessCountFunc = () => 0
        };

        await ol.Start();

        var results = new List<OllamaResponse>();

        try
        {
            await foreach (var r in ol.GenerateMessage([], "heavy-model"))
            {
                results.Add(r);
            }
        }
        catch (Exception)
        {
            // should not happen if heavyclient is used correctly
        }

        Assert.Single(results);
        Assert.Equal("Finally loaded!", results[0].Message?.Content);
        Assert.NotEqual(ServiceStatus.Stopped, ol.OllamaServiceState?.Status);
    }
}
