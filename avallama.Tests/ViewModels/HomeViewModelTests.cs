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

        SetupMock();

        var vm = CreateViewModel();

        var availableModels = new ObservableCollection<string>
        {
            "model1",
            "model2"
        };

        // Raise Connected status so the ViewModel doesn't wait indefinitely for connection
        fixture.OllamaMock.Raise(x =>
            x.ApiStatusChanged += null, new OllamaApiStatus(OllamaApiState.Connected));

        await vm.InitializeAsync();

        Assert.Equal(availableModels, vm.AvailableModels);
        Assert.Equal("model1", vm.SelectedModelName);
        Assert.True(vm.IsModelsDropdownEnabled);
    }

    [Fact]
    public async Task InitializeModels_WhenEmptyList_IsEmpty_SelectedEmptyString_DropdownDisabled()
    {
        fixture.OllamaMock.Reset();
        fixture.OllamaMock
            .Setup(o => o.GetDownloadedModelsAsync())
            .ReturnsAsync([]);

        SetupMock();

        var vm = CreateViewModel();

        // Raise Connected status so the ViewModel doesn't wait indefinitely for connection
        fixture.OllamaMock.Raise(x =>
            x.ApiStatusChanged += null, new OllamaApiStatus(OllamaApiState.Connected));

        await vm.InitializeAsync();

        Assert.Empty(vm.AvailableModels);
        Assert.False(vm.IsModelsDropdownEnabled);
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
            x.ApiStatusChanged += null, new OllamaApiStatus(OllamaApiState.Connected));

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
            x.ApiStatusChanged += null, new OllamaApiStatus(OllamaApiState.Connected));

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
            fixture.MessengerMock.Object
        );
    }
}
