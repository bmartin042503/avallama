// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using avallama.Constants.States;
using avallama.Models;
using avallama.Models.Ollama;
using avallama.Tests.Fixtures;
using avallama.ViewModels;
using avallama.Views;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Moq;
using Xunit;

namespace avallama.Tests.Views;

public class HomeViewTests : IClassFixture<TestServicesFixture>
{
    private readonly TestServicesFixture _fixture;

    public HomeViewTests(TestServicesFixture fixture)
    {
        _fixture = fixture;
        SetupDefaultBehaviors();
    }

    public void Dispose()
    {
        _fixture.OllamaMock.Reset();
        _fixture.ConfigMock.Reset();
        _fixture.DbMock.Reset();
        _fixture.DialogMock.Reset();
        _fixture.MessengerMock.Reset();
    }

    private void SetupDefaultBehaviors()
    {
        _fixture.ConfigMock
            .Setup(x => x.ReadSetting(It.IsAny<string>()))
            .Returns("");

        // raises Running process status so the ViewModel can also listen for API states (it's local connection by default)
        _fixture.OllamaMock.Raise(x =>
            x.ProcessStatusChanged += null, new OllamaProcessStatus(OllamaProcessLifecycle.Running));

        // raises Connected status so the ViewModel doesn't wait indefinitely for connection
        _fixture.OllamaApiClientMock.Raise(x =>
            x.StatusChanged += null, new OllamaApiStatus(OllamaConnectionState.Connected));

        _fixture.OllamaMock
            .Setup(x => x.GetDownloadedModelsAsync())
            .ReturnsAsync(new List<OllamaModel>());

        _fixture.DbMock.Setup(x => x.GetConversations()).ReturnsAsync([]);
        _fixture.DbMock.Setup(x => x.CreateConversation(It.IsAny<Conversation>())).ReturnsAsync(Guid.NewGuid());
        _fixture.DbMock.Setup(x => x.GetMessagesForConversation(It.IsAny<Conversation>())).ReturnsAsync([]);
    }

    private HomeViewModel CreateHomeViewModel()
    {
        return new HomeViewModel(
            _fixture.OllamaMock.Object,
            _fixture.DialogMock.Object,
            _fixture.ConfigMock.Object,
            _fixture.DbMock.Object,
            _fixture.UpdateMock.Object,
            _fixture.ModelCacheMock.Object,
            _fixture.MessengerMock.Object
        );
    }

    private (Window Window, HomeView View, HomeViewModel ViewModel) CreateAndShowHomeView()
    {
        var viewModel = CreateHomeViewModel();
        var view = new HomeView
        {
            DataContext = viewModel,
            IsFullScreenOverride = () => false
        };
        var window = new Window { Content = view };
        window.Show();
        return (window, view, viewModel);
    }

    private async Task<(Window Window, HomeView View, HomeViewModel ViewModel)> SetupInitializedHomeViewAsync(
        string selectedModelName)
    {
        var mockModels = new List<OllamaModel>
        {
            new() { Name = "test-model-1:8b", Size = 8_030_000_000 },
            new() { Name = "test-model-2:20b", Size = 20_100_000_000 }
        };

        _fixture.OllamaMock.Setup(x => x.GetDownloadedModelsAsync()).ReturnsAsync(mockModels);

        var (window, view, viewModel) = CreateAndShowHomeView();

        // raises Running process status so the ViewModel can also listen for API states (it's local connection by default)
        _fixture.OllamaMock.Raise(x =>
            x.ProcessStatusChanged += null, new OllamaProcessStatus(OllamaProcessLifecycle.Running));

        // Raise Connected status so the ViewModel doesn't wait indefinitely for connection
        _fixture.OllamaMock.Raise(x =>
            x.ApiStatusChanged += null, new OllamaApiStatus(OllamaConnectionState.Connected));

        await viewModel.InitializeAsync();

        viewModel.SelectedModelName = !string.IsNullOrEmpty(selectedModelName) ? selectedModelName : string.Empty;

        return (window, view, viewModel);
    }

    [AvaloniaFact]
    public void SideBar_ClickingSideBarButton_TogglesSideBarCorrectly()
    {
        var (_, view, _) = CreateAndShowHomeView();

        var sideBarButton = view.FindControl<Button>("SideBarButton");
        var sideBar = view.FindControl<Grid>("SideBar");
        var mainGrid = view.FindControl<Grid>("MainGrid");

        Assert.NotNull(sideBarButton);
        Assert.NotNull(sideBar);
        Assert.NotNull(mainGrid);

        // check if sidebar is opened by default when window is shown
        Assert.Contains(sideBar, mainGrid.Children);
        Assert.True(mainGrid.ColumnDefinitions[0].Width.Value > 0); // checking sidebar's width

        // simulate a click on sidebar button (close)
        sideBarButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

        // check if sidebar is closed after clicking the sidebar button
        Assert.DoesNotContain(sideBar, mainGrid.Children);
        Assert.Equal(0, mainGrid.ColumnDefinitions[0].Width.Value);

        // simulate another click on sidebar button (open)
        sideBarButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

        // check if sidebar is opened after clicking the sidebar button
        Assert.Contains(sideBar, mainGrid.Children);
        Assert.True(mainGrid.ColumnDefinitions[0].Width.Value > 0);
    }

    [AvaloniaFact]
    public void RetryPanel_WhenOllamaApiIsFaulted_ItAppearsCorrectly()
    {
        var (_, view, viewModel) = CreateAndShowHomeView();

        var conversationGrid = view.FindControl<Grid>("ConversationGrid");
        var retryButton = view.FindControl<Button>("RetryButton");
        var retryPanel = view.FindControl<StackPanel>("RetryPanel");
        var retryInfoText = view.FindControl<TextBlock>("RetryInfoText");

        Assert.NotNull(conversationGrid);
        Assert.NotNull(retryButton);
        Assert.NotNull(retryPanel);
        Assert.NotNull(retryInfoText);

        // raises Running process status so the ViewModel can also listen for API states (it's local connection by default)
        _fixture.OllamaMock.Raise(x =>
            x.ProcessStatusChanged += null, new OllamaProcessStatus(OllamaProcessLifecycle.Running));

        // set status to Faulted
        _fixture.OllamaMock.Raise(x =>
            x.ApiStatusChanged += null, new OllamaApiStatus(OllamaConnectionState.Faulted));
        Assert.True(viewModel.IsRetryPanelVisible);
        Assert.True(viewModel.IsRetryButtonVisible);
        Assert.True(retryButton.IsEnabled);
        Assert.True(retryButton.IsVisible);
        Assert.True(retryPanel.IsVisible);
        Assert.True(retryInfoText.IsVisible);
        Assert.False(string.IsNullOrEmpty(retryInfoText.Text));
        Assert.False(conversationGrid.IsVisible);
    }

    [AvaloniaFact]
    public void RetryPanel_WhenOllamaApiIsConnected_ItDisappearsCorrectly()
    {
        var (_, view, viewModel) = CreateAndShowHomeView();

        var conversationGrid = view.FindControl<Grid>("ConversationGrid");
        var retryButton = view.FindControl<Button>("RetryButton");
        var retryPanel = view.FindControl<StackPanel>("RetryPanel");
        var retryInfoText = view.FindControl<TextBlock>("RetryInfoText");

        Assert.NotNull(conversationGrid);
        Assert.NotNull(retryButton);
        Assert.NotNull(retryPanel);
        Assert.NotNull(retryInfoText);

        Assert.False(viewModel.IsRetryPanelVisible);
        Assert.False(viewModel.IsRetryButtonVisible);
        Assert.False(retryPanel.IsVisible);
        Assert.True(conversationGrid.IsVisible);
    }

    [AvaloniaFact]
    public void MessageTextBox_WhenOllamaServiceIsStoppedOrFailed_ItsDisabledCorrectly()
    {
        var (_, view, viewModel) = CreateAndShowHomeView();

        var messageTextBox = view.FindControl<TextBox>("MessageTextBox");

        Assert.NotNull(messageTextBox);

        _fixture.OllamaApiClientMock.Raise(x =>
            x.StatusChanged += null, new OllamaApiStatus(OllamaConnectionState.Disconnected));
        Assert.False(viewModel.IsMessageBoxEnabled);
        Assert.False(messageTextBox.IsEnabled);

        _fixture.OllamaApiClientMock.Raise(x =>
            x.StatusChanged += null, new OllamaApiStatus(OllamaConnectionState.Connected));

        _fixture.OllamaApiClientMock.Raise(x =>
            x.StatusChanged += null, new OllamaApiStatus(OllamaConnectionState.Faulted));
        Assert.False(viewModel.IsMessageBoxEnabled);
        Assert.False(messageTextBox.IsEnabled);
    }

    [AvaloniaFact]
    public async Task MessageTextBox_WhenNoDownloadedModelIsSelected_ItsDisabledCorrectly()
    {
        var (_, view, viewModel) = await SetupInitializedHomeViewAsync(string.Empty);

        var messageTextBox = view.FindControl<TextBox>("MessageTextBox");
        Assert.NotNull(messageTextBox);

        Assert.False(viewModel.IsMessageBoxEnabled);
        Assert.False(messageTextBox.IsEnabled);
    }

    [AvaloniaFact]
    public async Task MessageTextBox_WhenDownloadedModelIsSelected_ItsEnabledCorrectly()
    {
        var (_, view, viewModel) = await SetupInitializedHomeViewAsync("test-model-2:20b");

        var messageTextBox = view.FindControl<TextBox>("MessageTextBox");
        Assert.NotNull(messageTextBox);

        Assert.True(viewModel.IsMessageBoxEnabled);
        Assert.True(messageTextBox.IsEnabled);
        Assert.True(messageTextBox.IsVisible);
    }

    [AvaloniaFact]
    public async Task MessageTextBox_WhenDownloadedModelIsSelected_MessageEntered_ClearsTextBoxAndAddsMessages()
    {
        var (window, view, viewModel) = await SetupInitializedHomeViewAsync("test-model-1:8b");

        var messageTextBox = view.FindControl<TextBox>("MessageTextBox");
        Assert.NotNull(messageTextBox);
        Assert.NotNull(viewModel.SelectedConversation);

        const string testMessage = "This is a test message";

        messageTextBox.Focus();
        messageTextBox.Text = testMessage;
        window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);

        var userMessageIndex = viewModel.SelectedConversation.Messages.Count - 2;
        var generatedMessageIndex = viewModel.SelectedConversation.Messages.Count - 1;
        Assert.Empty(messageTextBox.Text);
        Assert.Equal(testMessage, viewModel.SelectedConversation.Messages[userMessageIndex].Content);
        Assert.True(viewModel.SelectedConversation.Messages[generatedMessageIndex] is GeneratedMessage);
    }

    [AvaloniaFact]
    public void ModelsComboBox_WithEmptyModelsList_ItsDisabledCorrectly()
    {
        _fixture.OllamaMock
            .Setup(x => x.GetDownloadedModelsAsync())
            .ReturnsAsync([]);

        var (_, view, _) = CreateAndShowHomeView();

        var modelsComboBox = view.FindControl<ComboBox>("ModelsComboBox");

        Assert.NotNull(modelsComboBox);
        Assert.False(modelsComboBox.IsEnabled);
        Assert.False(modelsComboBox.IsDropDownOpen);
    }

    // TODO: implement missing testcases such as
    // (ConversationScrollViewer) 'Scroll to bottom' button appears when scrolling down and configuration set to 'Floating button'
    // (ConversationScrollViewer) Scrolls to bottom correctly when scroll to bottom is clicked
    // (ConversationScrollViewer) Scrolls automatically to bottom when ScrollViewer height expands (new message) and configuration set to 'Automatic'
    // (ConversationScrollViewer) Scroll position stays still when ScrollViewer height expands (new message) and configration is set to 'None'
    // (ConversationItem) Conversation deletes correctly when right clicked on Conversation item and Delete is clicked
    // (NewConversationButton) New conversation button adds a conversation correctly
    // (ModelManagerButton) Opens ModelManagerView when clicking on Model Manager icon button
    // (SettingsButton) Opens SettingsView when clicking on Settings icon button
    // (ModelsComboBox) Selecting a model from the combobox changes selected model correctly in HomeViewModel
    // (ModelsComboBox) When opening existing conversation the last used model is selected (if available) correctly - this would fail atm
    // (MessageBox) Pressing enter with empty or whitespace message does not adds messages
    // (MessageBox) When message has whitespace characters the message is trimmed correctly
    // (InformationalMessages) When configuration is set to "true" informational messages appear correctly
    // (InformationalMessages) When configuration is set to "false" informational messages won't appear
    // etc.

    /* this is how to set up ConfigMock, so I won't forget syntax
       _fixture.ConfigMock
           .Setup(x => x.ReadSetting(It.IsAny<string>()))
           .Returns((string key) => key switch
           {
               ConfigurationKey.ShowInformationalMessages => "True",
               _ => ""
           });
    */
}
