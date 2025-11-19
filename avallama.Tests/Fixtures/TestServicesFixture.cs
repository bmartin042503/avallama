using avallama.Services;
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
}
