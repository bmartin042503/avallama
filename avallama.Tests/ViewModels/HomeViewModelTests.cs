// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using avallama.Constants.States;
using avallama.Models;
using avallama.Models.Dtos;
using avallama.Models.Ollama;
using avallama.Tests.Fixtures;
using avallama.ViewModels;
using Moq;
using Xunit;

namespace avallama.Tests.ViewModels;

public class HomeViewModelTests(TestServicesFixture fixture) : IClassFixture<TestServicesFixture>
{
    private void SetupMock()
    {
        // Mock db svc to prevent NullReferenceException when testing
        fixture.DbMock.Setup(db => db.GetConversations())
            .ReturnsAsync([]);
    }

    [Fact]
    public async Task InitializeModels_WhenNonEmptyList_HasItems_SelectedLastModel_DropdownEnabled()
    {
        fixture.OllamaMock.Reset();
        var models = new List<OllamaModel>
        {
            new() { Name = "model1" },
            new() { Name = "model2" }
        };

        fixture.OllamaMock
            .Setup(o => o.GetDownloadedModelsAsync())
            .ReturnsAsync(models);

        fixture.ModelCacheMock
            .Setup(o => o.GetDownloadedModelsAsync())
            .ReturnsAsync(models);

        SetupMock();

        var vm = CreateViewModel();

        var availableModels = new ObservableCollection<string>
        {
            "model1",
            "model2"
        };

        // raises Running process status so the ViewModel can also listen for API states (it's local connection by default)
        fixture.OllamaMock.Raise(x =>
            x.ProcessStatusChanged += null, new OllamaProcessStatus(OllamaProcessLifecycle.Running));

        // raises Connected API status so the ViewModel doesn't wait indefinitely for connection
        fixture.OllamaMock.Raise(x =>
            x.ApiStatusChanged += null, new OllamaApiStatus(OllamaConnectionState.Connected));

        await vm.InitializeAsync();

        Assert.Equal(availableModels, vm.AvailableModels);
        Assert.Equal("model1", vm.SelectedModelName);
        Assert.True(vm.IsModelsDropdownEnabled);
    }

    [Fact]
    public async Task TitleRegenerates_WhenFirstMessageExchangeOccurs()
    {
        fixture.OllamaMock.Reset();
        fixture.DbMock.Reset();

        var conv = new Conversation("A", string.Empty) { ConversationId = Guid.NewGuid() };

        fixture.OllamaMock
            .Setup(o => o.GenerateMessageAsync(
                It.IsAny<List<Message>>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(MainStreamAsync());

        var updatedConversationIds = new List<Guid>();
        fixture.DbMock.Setup(db => db.GetConversations()).ReturnsAsync([conv]);
        fixture.DbMock
            .Setup(db => db.UpdateConversationTitle(It.IsAny<Conversation>()))
            .Callback<Conversation>(c => updatedConversationIds.Add(c.ConversationId))
            .ReturnsAsync(true);

        var vm = CreateViewModel();

        // Raise Connected status so the ViewModel doesn't wait indefinitely for connection
        fixture.OllamaMock.Raise(x =>
            x.ApiStatusChanged += null, new OllamaApiStatus(OllamaConnectionState.Connected));

        await vm.InitializeAsync();

        vm.SelectedConversation = conv;
        vm.NewMessageText = "hello";

        await vm.SendMessageCommand.ExecuteAsync(null);

        Assert.Contains(conv.ConversationId, updatedConversationIds);
        fixture.DbMock.Verify(
            db => db.UpdateConversationTitle(It.Is<Conversation>(c => c.ConversationId == conv.ConversationId)),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task TitleGeneration_WhenUserSwitchesConversation_UpdatesRightConversation()
    {
        fixture.OllamaMock.Reset();
        fixture.DbMock.Reset();

        var convA = new Conversation("A", string.Empty) { ConversationId = Guid.NewGuid() };
        var convB = new Conversation("B", string.Empty) { ConversationId = Guid.NewGuid() };

        fixture.DbMock.Setup(db => db.GetConversations()).ReturnsAsync([convA, convB]);
        fixture.DbMock.Setup(db => db.GetMessagesForConversation(It.IsAny<Conversation>())).ReturnsAsync([]);
        fixture.DbMock.Setup(db =>
                db.InsertMessage(It.IsAny<Guid>(), It.IsAny<Message>(), It.IsAny<string?>(), It.IsAny<double?>()))
            .ReturnsAsync(1L);

        var allowTitleToFinish = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        fixture.OllamaMock
            .Setup(o => o.GenerateMessageAsync(
                It.IsAny<List<Message>>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns((List<Message> history, string _,  CancellationToken _) =>
            {
                var isTitleRequest =
                    history.Count > 0 &&
                    history[^1].Content.Contains(
                        "Generate only a single short title",
                        StringComparison.Ordinal);

                return isTitleRequest
                    ? TitleStreamAsync(allowTitleToFinish.Task)
                    : MainStreamAsync();
            });

        var updatedConversationIds = new List<Guid>();
        fixture.DbMock
            .Setup(db => db.UpdateConversationTitle(It.IsAny<Conversation>()))
            .Callback<Conversation>(c => updatedConversationIds.Add(c.ConversationId))
            .ReturnsAsync(true);

        var vm = CreateViewModel();

        // Raise Connected status so the ViewModel doesn't wait indefinitely for connection
        fixture.OllamaMock.Raise(x =>
            x.ApiStatusChanged += null, new OllamaApiStatus(OllamaConnectionState.Connected));

        await vm.InitializeAsync();

        vm.SelectedConversation = convA;

        vm.NewMessageText = "hello";

        var generateTask = vm.SendMessageCommand.ExecuteAsync(null);

        vm.SelectedConversation = convB;

        allowTitleToFinish.SetResult();
        await generateTask;

        Assert.Contains(convA.ConversationId, updatedConversationIds);
        Assert.DoesNotContain(convB.ConversationId, updatedConversationIds);

        fixture.DbMock.Verify(
            db => db.UpdateConversationTitle(It.Is<Conversation>(c => c.ConversationId == convA.ConversationId)),
            Times.AtLeastOnce);

        fixture.DbMock.Verify(
            db => db.UpdateConversationTitle(It.Is<Conversation>(c => c.ConversationId == convB.ConversationId)),
            Times.Never);
    }

    [Fact]
    public async Task DeleteMessage_WhenValidMessage_DeletesFromDatabaseAndRemovesFromCollection()
    {
        fixture.DbMock.Reset();
        var vm = CreateViewModel();

        var conversation = new Conversation("A", "model-1:1b") { ConversationId = Guid.NewGuid() };
        var message = new Message("Test message") { Id = 10 };
        conversation.Messages.Add(message);

        vm.SelectedConversation = conversation;

        await vm.DeleteMessageCommand.ExecuteAsync(message);

        fixture.DbMock.Verify(db => db.DeleteMessage(10), Times.Once);
        Assert.DoesNotContain(message, conversation.Messages);
    }

    [Fact]
    public async Task DeleteMessage_WhenFailedMessage_RemovesFromCollectionButDoesNotCallDatabase()
    {
        fixture.DbMock.Reset();
        var vm = CreateViewModel();

        var conversation = new Conversation("A", "model-1:1b") { ConversationId = Guid.NewGuid() };
        var failedMessage = new FailedMessage { Id = -1 };
        conversation.Messages.Add(failedMessage);

        vm.SelectedConversation = conversation;

        await vm.DeleteMessageCommand.ExecuteAsync(failedMessage);

        fixture.DbMock.Verify(db => db.DeleteMessage(It.IsAny<long>()), Times.Never);

        Assert.DoesNotContain(failedMessage, conversation.Messages);
    }

    [Fact]
    public async Task SearchBoxText_WhenChanged_FiltersConversationsCorrectly()
    {
        fixture.DbMock.Reset();
        fixture.OllamaMock.Reset();

        var conv1 = new Conversation("C# Programming", string.Empty) { ConversationId = Guid.NewGuid() };
        var conv2 = new Conversation("Python Scripts", string.Empty) { ConversationId = Guid.NewGuid() };
        var conv3 = new Conversation("Avalonia UI Design", string.Empty) { ConversationId = Guid.NewGuid() };

        fixture.DbMock.Setup(db => db.GetConversations()).ReturnsAsync([conv1, conv2, conv3]);

        var vm = CreateViewModel();

        fixture.OllamaMock.Raise(x =>
            x.ApiStatusChanged += null, new OllamaApiStatus(OllamaConnectionState.Connected));

        await vm.InitializeAsync();

        Assert.Equal(3, vm.Conversations?.Count);

        vm.SearchBoxText = "Python";

        Assert.NotNull(vm.Conversations);
        Assert.Single(vm.Conversations);
        Assert.Contains(conv2, vm.Conversations);
    }

    [Fact]
    public async Task SearchBoxText_WhenCleared_RestoresAllConversations()
    {
        fixture.DbMock.Reset();
        fixture.OllamaMock.Reset();

        var conv1 = new Conversation("C# Programming", string.Empty) { ConversationId = Guid.NewGuid() };
        var conv2 = new Conversation("Python Scripts", string.Empty) { ConversationId = Guid.NewGuid() };

        fixture.DbMock.Setup(db => db.GetConversations()).ReturnsAsync([conv1, conv2]);

        var vm = CreateViewModel();

        fixture.OllamaMock.Raise(x =>
            x.ApiStatusChanged += null, new OllamaApiStatus(OllamaConnectionState.Connected));

        await vm.InitializeAsync();

        vm.SearchBoxText = "C#";
        vm.SearchBoxText = string.Empty;

        Assert.NotNull(vm.Conversations);
        Assert.Equal(2, vm.Conversations.Count);
        Assert.Contains(conv1, vm.Conversations);
        Assert.Contains(conv2, vm.Conversations);
    }

    private static async IAsyncEnumerable<OllamaResponse> MainStreamAsync()
    {
        yield return new OllamaResponse
        {
            Message = new MessageContent { Content = "assistant chunk" },
            EvalCount = 10,
            EvalDuration = 1_000_000_000
        };

        await Task.CompletedTask;
    }

    private static async IAsyncEnumerable<OllamaResponse> TitleStreamAsync(Task gate)
    {
        yield return new OllamaResponse
        {
            Message = new MessageContent { Content = "Right conversation title" }
        };

        await gate.ConfigureAwait(false);
    }

    private HomeViewModel CreateViewModel()
    {
        return new HomeViewModel(
            fixture.OllamaMock.Object,
            fixture.DialogMock.Object,
            fixture.ConfigMock.Object,
            fixture.DbMock.Object,
            fixture.UpdateMock.Object,
            fixture.ModelCacheMock.Object,
            fixture.MessengerMock.Object
        );
    }
}
