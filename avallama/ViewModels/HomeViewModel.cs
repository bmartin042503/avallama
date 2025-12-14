// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using avallama.Constants;
using avallama.Models;
using avallama.Services;
using avallama.Utilities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

namespace avallama.ViewModels;

public partial class HomeViewModel : PageViewModel
{
    private const string OllamaDownloadUrl = @"https://ollama.com/download/";

    public string ResourceLimitWarning { get; } = string.Format(LocalizationService.GetString("LOW_VRAM_WARNING"));

    public string NoModelsDownloadedWarning { get; } =
        string.Format(LocalizationService.GetString("NOT_DOWNLOADED_WARNING"));

    private readonly IOllamaService _ollamaService;
    private readonly IDialogService _dialogService;
    private readonly IConfigurationService _configurationService;
    private readonly IConversationService _conversationService;

    private readonly TaskCompletionSource<bool> _ollamaServerStarted = new();
    private readonly IMessenger _messenger;

    public string ScrollSetting = string.Empty;

    private ObservableStack<Conversation> _conversations = null!;

    public ObservableStack<Conversation> Conversations
    {
        get => _conversations;
        set => SetProperty(ref _conversations, value);
    }

    private readonly ConditionalWeakTable<Conversation, ConversationState> _conversationStates = new();

    private ObservableCollection<string> _availableModels;

    public ObservableCollection<string> AvailableModels
    {
        get => _availableModels;
        set => SetProperty(ref _availableModels, value);
    }

    [ObservableProperty] private string _newMessageText = string.Empty;
    [ObservableProperty] private bool _isResourceWarningVisible;
    [ObservableProperty] private bool _isNoModelsWarningVisible;
    [ObservableProperty] private bool _isMessageBoxEnabled;
    [ObservableProperty] private Conversation? _selectedConversation;
    [ObservableProperty] private string _remoteConnectionText = string.Empty;
    [ObservableProperty] private bool _isRemoteConnectionTextVisible;
    [ObservableProperty] private bool _isRetryPanelVisible;
    [ObservableProperty] private bool _isRetryButtonVisible;
    [ObservableProperty] private string _retryInfoText = string.Empty;
    [ObservableProperty] private bool _showInformationalMessages;
    [ObservableProperty] private bool _isModelsDropdownEnabled = true;

    private bool _isInitializedAsync;

    public string SelectedModelName
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    } = string.Empty;

    [RelayCommand]
    private async Task SendMessage()
    {
        if (NewMessageText.Length == 0 || SelectedConversation == null) return;
        NewMessageText = NewMessageText.Trim();
        var message = new Message(NewMessageText);
        SelectedConversation.AddMessage(message);
        await _conversationService.InsertMessage(SelectedConversation.ConversationId, message, null, null);
        NewMessageText = string.Empty;
        if (Conversations.IndexOf(SelectedConversation) != 0)
        {
            Conversations.Remove(SelectedConversation);
            Conversations.Push(SelectedConversation);
        }

        await AddGeneratedMessage();
    }

    [RelayCommand]
    public async Task CreateNewConversation()
    {
        var newConversation = new Conversation(
            LocalizationService.GetString("NEW_CONVERSATION"),
            string.Empty
        );
        newConversation.ConversationId = await _conversationService.CreateConversation(newConversation);
        Conversations.Push(newConversation);
        SelectedConversation = newConversation;
    }

    [RelayCommand]
    public async Task SelectConversation(object parameter)
    {
        if (parameter is not Guid guid) return;

        if (guid == SelectedConversation?.ConversationId) return;

        var selectedConversation = Conversations.FirstOrDefault(x => x.ConversationId == guid);
        if (selectedConversation == null) return;

        SelectedConversation = selectedConversation;

        var state = GetState(SelectedConversation);
        if (state.MessagesLoaded) return;

        SelectedConversation.Messages = await _conversationService.GetMessagesForConversation(SelectedConversation);
        state.MessagesLoaded = true;
    }

    [RelayCommand]
    public async Task DeleteConversation(object parameter)
    {
        if (parameter is not Guid guid || SelectedConversation == null) return;

        var res = await _dialogService.ShowConfirmationDialog(
            LocalizationService.GetString("CONFIRM_DELETION_DIALOG_TITLE"), LocalizationService.GetString("DELETE"),
            LocalizationService.GetString("CANCEL"),
            string.Format(LocalizationService.GetString("CONFIRM_DELETION_DIALOG_DESC"),
                LocalizationService.GetString("THIS_CONVERSATION")), ConfirmationType.Positive);

        if (res is ConfirmationResult { Confirmation: ConfirmationType.Negative }) return;

        await _conversationService.DeleteConversation(guid);
        Conversations.Remove(SelectedConversation);

        if (Conversations.Count == 0)
        {
            await CreateNewConversation();
            return;
        }

        var newIndex = Math.Min(Conversations.IndexOf(SelectedConversation) + 1, Conversations.Count - 1);
        SelectedConversation = Conversations[newIndex];
    }

    [RelayCommand]
    public async Task RetryOllamaConnection()
    {
        await _ollamaService.Retry();
    }

    private async Task AddGeneratedMessage()
    {
        if (SelectedConversation == null) return;
        var generatedMessage = new GeneratedMessage("", 0.0);
        SelectedConversation.AddMessage(generatedMessage);
        var messageHistory = new List<Message>(SelectedConversation.Messages.ToList());
        messageHistory.RemoveAt(messageHistory.Count - 1);

        // hibás (nem generált) üzenetek kitörlése
        messageHistory.RemoveAll(message => message is FailedMessage);

        await foreach (var chunk in _ollamaService.GenerateMessage(messageHistory, SelectedModelName))
        {
            if (chunk.Message != null) generatedMessage.Content += chunk.Message.Content;

            if (chunk is { EvalCount: not null, EvalDuration: not null })
            {
                double tokensPerSecond =
                    chunk.EvalCount.GetValueOrDefault() / (double)chunk.EvalDuration * Math.Pow(10, 9);
                generatedMessage.GenerationSpeed = tokensPerSecond;
                IsResourceWarningVisible = tokensPerSecond < 20;
                SelectedConversation.MessageCountToRegenerateTitle++;
                if (SelectedConversation.MessageCountToRegenerateTitle == 3)
                {
                    await RegenerateConversationTitle();
                }
            }
        }

        await _conversationService.InsertMessage(SelectedConversation.ConversationId, generatedMessage,
            SelectedModelName, generatedMessage.GenerationSpeed);

        SelectedConversation.Model = SelectedModelName;
    }

    private async Task RegenerateConversationTitle()
    {
        if (SelectedConversation == null) return;
        SelectedConversation.Title = string.Empty;

        // TODO: better solution for title generation (not working for thinking models etc.)
        const string request =
            "Generate only a single short title for this conversation with no use of quotation marks.";

        var tmpMessage = new Message(request);
        var messageHistory = new List<Message>(SelectedConversation.Messages.ToList()) { tmpMessage };
        await foreach (var chunk in _ollamaService.GenerateMessage(messageHistory, SelectedModelName))
        {
            if (chunk.Message != null) SelectedConversation.Title += chunk.Message.Content;
        }

        SelectedConversation.MessageCountToRegenerateTitle = 0;
        await _conversationService.UpdateConversationTitle(SelectedConversation);
    }

    public HomeViewModel(
        IOllamaService ollamaService,
        IDialogService dialogService,
        IConfigurationService configurationService,
        IConversationService conversationService,
        IMessenger messenger
    )
    {
        Page = ApplicationPage.Home;

        _dialogService = dialogService;
        _ollamaService = ollamaService;
        _configurationService = configurationService;
        _conversationService = conversationService;
        _messenger = messenger;
        _availableModels = [];

        _messenger.Register<ApplicationMessage.ReloadSettings>(this, (_, _) => { LoadSettings(); });

        LoadSettings();

        _ollamaService.ServiceStateChanged += OllamaServiceStateChanged;
        if (_ollamaService.OllamaServiceState != null)
        {
            OllamaServiceStateChanged(_ollamaService.OllamaServiceState);
        }
    }

    public async Task InitializeAsync()
    {
        try
        {
            var tmpName = SelectedModelName;
            AvailableModels.Clear();
            SelectedModelName = tmpName;

            if (!_isInitializedAsync) await InitializeConversations();

            await InitializeModels();

            _isInitializedAsync = true;
        }
        catch (Exception ex)
        {
            // TODO: proper logging
            Console.WriteLine(ex);
        }
    }

    private void LoadSettings()
    {
        ScrollSetting = _configurationService.ReadSetting(ConfigurationKey.ScrollToBottom);
        ShowInformationalMessages =
            _configurationService.ReadSetting(ConfigurationKey.ShowInformationalMessages) == "True";
    }

    private void RedirectToOllamaDownload()
    {
        var processUrl = OllamaDownloadUrl;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            processUrl += "windows";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            processUrl += "linux";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            processUrl += "mac";
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = processUrl,
            UseShellExecute = true
        });
    }

    // TODO: proper logging
    private void OllamaServiceStateChanged(ServiceState? state)
    {
        if (state == null) return;
        switch (state.Status)
        {
            case ServiceStatus.Running:
                var apiHost = _configurationService.ReadSetting(ConfigurationKey.ApiHost);
                var apiPort = _configurationService.ReadSetting(ConfigurationKey.ApiPort);

                // "remote connection" message if the app isn't connected to local ollama server
                if (!string.IsNullOrEmpty(apiHost) && apiHost != "localhost" && apiHost != "127.0.0.1")
                {
                    RemoteConnectionText = string.Format(LocalizationService.GetString("REMOTE_CONNECTION"),
                        apiHost + ":" + apiPort);
                    IsRemoteConnectionTextVisible = true;
                }
                else
                {
                    IsRemoteConnectionTextVisible = false;
                }

                if (_ollamaServerStarted.Task.Status != TaskStatus.RanToCompletion)
                    _ollamaServerStarted.SetResult(true);
                IsRetryPanelVisible = false;
                break;
            case ServiceStatus.NotInstalled:
                _dialogService.ShowActionDialog(
                    title: LocalizationService.GetString("OLLAMA_NOT_INSTALLED"),
                    actionButtonText: LocalizationService.GetString("DOWNLOAD"),
                    action: () =>
                    {
                        RedirectToOllamaDownload();

                        // message to AppService to shut down the app
                        _messenger.Send(new ApplicationMessage.Shutdown());
                    },
                    closeAction: () => { _messenger.Send(new ApplicationMessage.Shutdown()); },
                    description: LocalizationService.GetString("OLLAMA_NOT_INSTALLED_DESC"),
                    false
                );
                break;
            case ServiceStatus.Retrying:
                RetryInfoText = LocalizationService.GetString("CONNECTING");
                IsRetryPanelVisible = true;
                IsRetryButtonVisible = false;
                break;
            case ServiceStatus.Stopped or ServiceStatus.Failed:

                // if the last message meant to be a generated one, then we'll replace it to a FailedMessage
                if (SelectedConversation is { Messages.Count: > 0 })
                {
                    if (SelectedConversation.Messages.Last() is GeneratedMessage generatedMessage)
                    {
                        SelectedConversation.Messages.Remove(generatedMessage);
                        SelectedConversation.Messages.Add(new FailedMessage());
                    }
                }

                RetryInfoText = state.Message ?? LocalizationService.GetString("OLLAMA_CONNECTION_ERROR");
                IsRemoteConnectionTextVisible = false;
                IsRetryPanelVisible = true;
                IsRetryButtonVisible = true;
                break;
        }
    }

    private async Task InitializeConversations()
    {
        Conversations = await _conversationService.GetConversations();
        if (_conversations.Count <= 0)
        {
            await CreateNewConversation();
            return;
        }

        SelectedConversation = _conversations.FirstOrDefault()!;
        SelectedConversation.Messages = await _conversationService.GetMessagesForConversation(SelectedConversation);
        GetState(SelectedConversation).MessagesLoaded = true;
    }

    private ConversationState GetState(Conversation conversation)
    {
        return _conversationStates.GetOrCreateValue(conversation);
    }

    public async Task InitializeModels(bool test = false)
    {
        // wait for ollama to start
        if (!test)
        {
            await _ollamaServerStarted.Task;
        }

        var previouslySelectedModelName = string.Empty;
        if (_isInitializedAsync && !string.IsNullOrEmpty(SelectedModelName)
                                && SelectedModelName != LocalizationService.GetString("LOADING_MODELS")
                                && SelectedModelName != LocalizationService.GetString("NO_MODELS_FOUND"))
        {
            previouslySelectedModelName = SelectedModelName;
        }

        IsModelsDropdownEnabled = false;
        AvailableModels.Add(LocalizationService.GetString("LOADING_MODELS"));
        SelectedModelName = AvailableModels[0];
        var downloadedModels = await _ollamaService.GetDownloadedModels();
        if (downloadedModels.Count == 0)
        {
            AvailableModels.Clear();
            AvailableModels.Add(LocalizationService.GetString("NO_MODELS_FOUND"));
            SelectedModelName = AvailableModels[0];
            IsModelsDropdownEnabled = false;
            IsNoModelsWarningVisible = true;
            IsMessageBoxEnabled = false;
            return;
        }

        foreach (var model in downloadedModels)
        {
            AvailableModels.Add(model.Name);
        }

        AvailableModels.RemoveAt(0);
        IsModelsDropdownEnabled = true;
        IsNoModelsWarningVisible = false;
        IsMessageBoxEnabled = true;

        var sorted = AvailableModels.OrderBy(x => x).ToList();
        AvailableModels.Clear();
        foreach (var item in sorted)
            AvailableModels.Add(item);

        if (!_isInitializedAsync) SelectedModelName = AvailableModels[0];

        if (!string.IsNullOrEmpty(previouslySelectedModelName)) SelectedModelName = previouslySelectedModelName;
    }
}

// Helper class to save the state of a conversation object, so that DB queries only happen when selecting a conversation
// that has not yet been selected, since once messages are loaded, they are available in memory in the viewmodel
internal class ConversationState
{
    public bool MessagesLoaded { get; set; }
}
