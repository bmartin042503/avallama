// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using avallama.Services;
using avallama.Services.Ollama;
using avallama.Services.Persistence;
using avallama.Services.Queue;
using avallama.Utilities.Network;
using CommunityToolkit.Mvvm.Messaging;
using Moq;

namespace avallama.Tests.Fixtures;

public class TestServicesFixture
{
    public Mock<IOllamaService> OllamaMock { get; } = new();
    public Mock<IDialogService> DialogMock { get; } = new();
    public Mock<IConfigurationService> ConfigMock { get; } = new();
    public Mock<IConversationService> DbMock { get; } = new();
    public Mock<IMessenger> MessengerMock { get; } = new();
    public Mock<IModelCacheService> ModelCacheMock { get; } = new();
    public Mock<IOllamaScraper> ScraperMock { get; } = new();
    public Mock<IModelDownloadQueueService> DownloadQueueMock { get; } = new();
    public Mock<INetworkManager> NetworkManagerMock { get; } = new();
    public Mock<IUpdateService> UpdateMock { get; } = new();
}
