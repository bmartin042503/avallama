// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System.Collections.Generic;
using System.Threading.Tasks;
using avallama.Models;
using avallama.Services;
using avallama.Tests.Fixtures;
using avallama.ViewModels;
using avallama.Views;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using CommunityToolkit.Mvvm.Messaging;
using Moq;
using Xunit;

namespace avallama.Tests.Views;

public class HomeViewTests : IClassFixture<TestServicesFixture>
{
    private readonly TestServicesFixture _fixture;

    public HomeViewTests(TestServicesFixture fixture)
    {
        _fixture = fixture;

        _fixture.ConfigMock
            .Setup(x => x.ReadSetting(It.IsAny<string>()))
            .Returns("");
    }

    // this is needed so we can override InitializeAsync() to use it for testing
    // so it won't be necessary to call it again when doing UI testing since OnAttachedToVisualTree will be called
    // when testing ViewModels calling InitializeAsync() might be necessary still
    private class TestHomeViewModel(
        IOllamaService ollama,
        IDialogService dialog,
        IConfigurationService config,
        IConversationService db,
        IMessenger messenger)
        : HomeViewModel(ollama, dialog, config, db, messenger)
    {
        public override Task InitializeAsync(bool test = false)
        {
            return base.InitializeAsync(true);
        }
    }

    private TestHomeViewModel CreateTestHomeViewModel()
    {
        return new TestHomeViewModel(
            _fixture.OllamaMock.Object,
            _fixture.DialogMock.Object,
            _fixture.ConfigMock.Object,
            _fixture.DbMock.Object,
            _fixture.MessengerMock.Object
        );
    }

    [AvaloniaFact]
    public void SideBar_ClickingSideBarButton_TogglesSideBarCorrectly()
    {
        var viewModel = CreateTestHomeViewModel();
        var view = new HomeView { DataContext = viewModel };
        var window = new Window { Content = view };
        window.Show();

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
    public void RetryPanel_WhenOllamaServiceIsStoppedOrFailed_AppearsCorrectly()
    {
        var viewModel = CreateTestHomeViewModel();
        var view = new HomeView { DataContext = viewModel };
        var window = new Window { Content = view };
        window.Show();

        var conversationGrid = view.FindControl<Grid>("ConversationGrid");
        var retryButton = view.FindControl<Button>("RetryButton");
        var retryPanel = view.FindControl<StackPanel>("RetryPanel");
        var retryInfoText = view.FindControl<TextBlock>("RetryInfoText");

        Assert.NotNull(conversationGrid);
        Assert.NotNull(retryButton);
        Assert.NotNull(retryPanel);
        Assert.NotNull(retryInfoText);

        // ollama service stops
        _fixture.OllamaMock.Raise(x => x.ServiceStateChanged += null, new ServiceState(ServiceStatus.Stopped));
        Assert.True(viewModel.IsRetryPanelVisible);
        Assert.True(viewModel.IsRetryButtonVisible);
        Assert.True(retryButton.IsEnabled);
        Assert.True(retryButton.IsVisible);
        Assert.True(retryPanel.IsVisible);
        Assert.True(retryInfoText.IsVisible);
        Assert.False(string.IsNullOrEmpty(retryInfoText.Text));
        Assert.False(conversationGrid.IsVisible);

        // ollama service is running (so we can see if it sets view for "failed" status correctly too)
        _fixture.OllamaMock.Raise(x => x.ServiceStateChanged += null, new ServiceState(ServiceStatus.Running));

        // ollama service fails
        _fixture.OllamaMock.Raise(x => x.ServiceStateChanged += null, new ServiceState(ServiceStatus.Failed));
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
    public void RetryPanel_WhenOllamaServiceIsRunning_DisappearsCorrectly()
    {
        var viewModel = CreateTestHomeViewModel();
        var view = new HomeView { DataContext = viewModel };
        var window = new Window { Content = view };
        window.Show();

        var conversationGrid = view.FindControl<Grid>("ConversationGrid");
        var retryButton = view.FindControl<Button>("RetryButton");
        var retryPanel = view.FindControl<StackPanel>("RetryPanel");
        var retryInfoText = view.FindControl<TextBlock>("RetryInfoText");

        Assert.NotNull(conversationGrid);
        Assert.NotNull(retryButton);
        Assert.NotNull(retryPanel);
        Assert.NotNull(retryInfoText);

        _fixture.OllamaMock.Raise(x => x.ServiceStateChanged += null, new ServiceState(ServiceStatus.Running));
        Assert.False(viewModel.IsRetryPanelVisible);
        Assert.False(viewModel.IsRetryButtonVisible);
        Assert.False(retryPanel.IsVisible);
        Assert.True(conversationGrid.IsVisible);
    }

    [AvaloniaFact]
    public void MessageTextBox_WhenOllamaServiceIsStoppedOrFailed_ItsDisabledCorrectly()
    {
        var viewModel = CreateTestHomeViewModel();
        var view = new HomeView { DataContext = viewModel };
        var window = new Window { Content = view };
        window.Show();

        var messageTextBox = view.FindControl<TextBox>("MessageTextBox");

        Assert.NotNull(messageTextBox);

        _fixture.OllamaMock.Raise(x => x.ServiceStateChanged += null, new ServiceState(ServiceStatus.Stopped));
        Assert.False(viewModel.IsMessageBoxEnabled);
        Assert.False(messageTextBox.IsEnabled);

        _fixture.OllamaMock.Raise(x => x.ServiceStateChanged += null, new ServiceState(ServiceStatus.Running));

        _fixture.OllamaMock.Raise(x => x.ServiceStateChanged += null, new ServiceState(ServiceStatus.Failed));
        Assert.False(viewModel.IsMessageBoxEnabled);
        Assert.False(messageTextBox.IsEnabled);
    }

    [AvaloniaFact]
    public void MessageTextBox_WithNoDownloadedModelSelected_ItsDisabledCorrectly()
    {
        var viewModel = CreateTestHomeViewModel();
        var view = new HomeView { DataContext = viewModel };
        var window = new Window { Content = view };
        window.Show();

        var mockModels = new List<OllamaModel>
        {
            new()
            {
                Name = "test-model-1:8b",
                Size = 8_030_000_000,
                DownloadStatus = ModelDownloadStatus.Downloaded
            },
            new()
            {
                Name = "test-model-2:20b",
                Size = 20_100_000_000,
                DownloadStatus = ModelDownloadStatus.Downloaded
            }
        };

        _fixture.OllamaMock
            .Setup(x => x.GetDownloadedModels(It.IsAny<bool>()))
            .ReturnsAsync(mockModels);

        viewModel.SelectedModelName = string.Empty;

        var messageTextBox = view.FindControl<TextBox>("MessageTextBox");
        Assert.NotNull(messageTextBox);

        // we use Raise and not RaiseAsync cause the delegate in OllamaService returns void not a Task
        _fixture.OllamaMock.Raise(x => x.ServiceStateChanged += null, new ServiceState(ServiceStatus.Running));

        Assert.False(viewModel.IsMessageBoxEnabled);
        Assert.False(messageTextBox.IsEnabled);
    }

    [AvaloniaFact]
    public async Task MessageTextBox_WithDownloadedModelSelected_ItsEnabledCorrectly()
    {
        var viewModel = CreateTestHomeViewModel();
        var view = new HomeView { DataContext = viewModel };
        var window = new Window { Content = view };
        window.Show();

        var mockModels = new List<OllamaModel>
        {
            new()
            {
                Name = "test-model-1:8b",
                Size = 8_030_000_000,
                DownloadStatus = ModelDownloadStatus.Downloaded
            },
            new()
            {
                Name = "test-model-2:20b",
                Size = 20_100_000_000,
                DownloadStatus = ModelDownloadStatus.Downloaded
            }
        };

        _fixture.OllamaMock
            .Setup(x => x.GetDownloadedModels(It.IsAny<bool>()))
            .ReturnsAsync(mockModels);

        await viewModel.InitializeAsync(test: true);
        viewModel.SelectedModelName = "test-model-2:20b";

        // we use Raise and not RaiseAsync cause the delegate in OllamaService returns void not a Task
        _fixture.OllamaMock.Raise(x => x.ServiceStateChanged += null, new ServiceState(ServiceStatus.Running));

        var messageTextBox = view.FindControl<TextBox>("MessageTextBox");
        Assert.NotNull(messageTextBox);

        Assert.True(viewModel.IsMessageBoxEnabled);
        Assert.True(messageTextBox.IsEnabled);
        Assert.True(messageTextBox.IsVisible);
    }

    [AvaloniaFact]
    public async Task MessageTextBox_WhenDownloadedModelIsSelected_MessageEntered_ClearsTextBoxAndAddsMessages()
    {
        var viewModel = CreateTestHomeViewModel();
        var view = new HomeView { DataContext = viewModel };
        var window = new Window { Content = view };
        window.Show();

        var mockModels = new List<OllamaModel>
        {
            new()
            {
                Name = "test-model-1:8b",
                Size = 8_030_000_000,
                DownloadStatus = ModelDownloadStatus.Downloaded
            },
            new()
            {
                Name = "test-model-2:20b",
                Size = 20_100_000_000,
                DownloadStatus = ModelDownloadStatus.Downloaded
            }
        };

        _fixture.OllamaMock
            .Setup(x => x.GetDownloadedModels(It.IsAny<bool>()))
            .ReturnsAsync(mockModels);

        await viewModel.InitializeAsync(test: true);
        viewModel.SelectedModelName = "test-model-1:8b";

        // we use Raise and not RaiseAsync cause the delegate in OllamaService returns void not a Task
        _fixture.OllamaMock.Raise(x => x.ServiceStateChanged += null, new ServiceState(ServiceStatus.Running));

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
    // (ModelsComboBox) When opening existing conversation the last model used is selected (if available) correctly - this would fail atm
    // (MessageBox) Pressing enter with empty or whitespace message does not adds messages
    // (MessageBox) When message has whitespace characters the message is trimmed correctly
    // etc.
}
