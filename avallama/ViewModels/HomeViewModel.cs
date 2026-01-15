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

/// <summary>
/// ViewModel for the Home page, managing chat conversations, model selection,
/// and interactions with the Ollama service.
/// </summary>
public partial class HomeViewModel : PageViewModel
{
    #region Constants & Fields

    private const string OllamaDownloadUrl = @"https://ollama.com/download/";

    // Dependencies
    private readonly IOllamaService _ollamaService;
    private readonly IDialogService _dialogService;
    private readonly IConfigurationService _configurationService;
    private readonly IConversationService _conversationService;
    private readonly IMessenger _messenger;

    // Internal State
    private readonly ConditionalWeakTable<Conversation, ConversationState> _conversationStates = new();
    private bool _isInitializedAsync;

    // Backing fields for manual properties
    private ObservableStack<Conversation>? _conversations;
    private ObservableCollection<string> _availableModels;

    #endregion

    #region Properties

    /// <summary>
    /// Localized warning message for low VRAM situations.
    /// </summary>
    public string ResourceLimitWarning { get; } = string.Format(LocalizationService.GetString("LOW_VRAM_WARNING"));

    /// <summary>
    /// Localized warning message when no models are downloaded.
    /// </summary>
    public string NoModelsDownloadedWarning { get; } =
        string.Format(LocalizationService.GetString("NOT_DOWNLOADED_WARNING"));

    /// <summary>
    /// Configuration setting for scrolling behavior.
    /// </summary>
    public string ScrollSetting = string.Empty;

    /// <summary>
    /// Stack of chat conversations.
    /// </summary>
    public ObservableStack<Conversation>? Conversations
    {
        get => _conversations;
        set => SetProperty(ref _conversations, value);
    }

    /// <summary>
    /// Gets or sets the name of the currently selected Ollama model.
    /// </summary>
    public string SelectedModelName
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();

            IsMessageBoxEnabled = !string.IsNullOrEmpty(value);
        }
    } = string.Empty;

    /// <summary>
    /// The collection of available Ollama models.
    /// </summary>
    public ObservableCollection<string> AvailableModels
    {
        get => _availableModels;
        set => SetProperty(ref _availableModels, value);
    }

    [ObservableProperty] private string _newMessageText = string.Empty;
    [ObservableProperty] private Conversation? _selectedConversation;

    // UI Visibility & State Flags
    [ObservableProperty] private bool _isResourceWarningVisible;
    [ObservableProperty] private bool _isNoModelsWarningVisible;
    [ObservableProperty] private bool _isMessageBoxEnabled;
    [ObservableProperty] private string _remoteConnectionText = string.Empty;
    [ObservableProperty] private bool _isRemoteConnectionTextVisible;
    [ObservableProperty] private bool _isRetryPanelVisible;
    [ObservableProperty] private bool _isRetryButtonVisible;
    [ObservableProperty] private string _retryInfoText = string.Empty;
    [ObservableProperty] private bool _showInformationalMessages;
    [ObservableProperty] private bool _isModelsDropdownEnabled;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="HomeViewModel"/> class.
    /// </summary>
    /// <param name="ollamaService">Service for Ollama interactions.</param>
    /// <param name="dialogService">Service for displaying dialogs.</param>
    /// <param name="configurationService">Service for application settings.</param>
    /// <param name="conversationService">Service for conversation interactions.</param>
    /// <param name="messenger">Messenger for cross-component communication.</param>
    public HomeViewModel(
        IOllamaService ollamaService,
        IDialogService dialogService,
        IConfigurationService configurationService,
        IConversationService conversationService,
        IMessenger messenger
    )
    {
        Page = ApplicationPage.Home;

        _ollamaService = ollamaService;
        _dialogService = dialogService;
        _configurationService = configurationService;
        _conversationService = conversationService;
        _messenger = messenger;
        _availableModels = [];

        _messenger.Register<ApplicationMessage.ReloadSettings>(this, (_, _) => { LoadSettings(); });

        LoadSettings();

        _ollamaService.ServiceStateChanged += OllamaServiceStateChanged;
    }

    #endregion

    #region Commands

    /// <summary>
    /// Performs asynchronous initialization of conversations and models.
    /// Typically called when the view is attached to the visual tree.
    /// </summary>
    [RelayCommand]
    public async Task InitializeAsync()
    {
        try
        {
            if (!_isInitializedAsync)
            {
                await InitializeConversations();
            }

            await InitializeModels();
            if(!_isInitializedAsync) _isInitializedAsync = true;
        }
        catch (Exception ex)
        {
            // TODO: proper logging
        }
    }

    /// <summary>
    /// Sends the current message text to the conversation, saves it to the database,
    /// and triggers the model response generation.
    /// </summary>
    [RelayCommand]
    private async Task SendMessage()
    {
        if (NewMessageText.Length == 0 || SelectedConversation == null || Conversations == null) return;

        NewMessageText = NewMessageText.Trim();
        var message = new Message(NewMessageText);

        SelectedConversation.AddMessage(message);
        await _conversationService.InsertMessage(SelectedConversation.ConversationId, message, null, null);

        NewMessageText = string.Empty;

        // Move conversation to top
        if (Conversations.IndexOf(SelectedConversation) != 0)
        {
            Conversations.Remove(SelectedConversation);
            Conversations.Push(SelectedConversation);
        }

        await AddGeneratedMessage(SelectedConversation);
    }

    /// <summary>
    /// Creates a new empty conversation and selects it.
    /// </summary>
    [RelayCommand]
    public async Task CreateNewConversation()
    {
        Conversations ??= [];
        var newConversation = new Conversation(
            LocalizationService.GetString("NEW_CONVERSATION"),
            string.Empty
        );
        newConversation.ConversationId = await _conversationService.CreateConversation(newConversation);
        Conversations.Push(newConversation);
        SelectedConversation = newConversation;
    }

    /// <summary>
    /// Selects a conversation by its ID and loads its messages if not already loaded.
    /// </summary>
    /// <param name="parameter">The GUID of the conversation to select.</param>
    [RelayCommand]
    public async Task SelectConversation(object parameter)
    {
        if (parameter is not Guid guid || Conversations == null) return;

        if (guid == SelectedConversation?.ConversationId) return;

        var selectedConversation = Conversations.FirstOrDefault(x => x.ConversationId == guid);
        if (selectedConversation == null) return;

        SelectedConversation = selectedConversation;

        var state = GetState(SelectedConversation);
        if (state.MessagesLoaded) return;

        SelectedConversation.Messages = await _conversationService.GetMessagesForConversation(SelectedConversation);
        state.MessagesLoaded = true;
    }

    /// <summary>
    /// Deletes the specified conversation after user confirmation.
    /// </summary>
    /// <param name="parameter">The GUID of the conversation to delete.</param>
    [RelayCommand]
    public async Task DeleteConversation(object parameter)
    {
        if (parameter is not Guid guid || SelectedConversation == null || Conversations == null) return;

        var res = await _dialogService.ShowConfirmationDialog(
            LocalizationService.GetString("CONFIRM_DELETION_DIALOG_TITLE"),
            LocalizationService.GetString("DELETE"),
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

    /// <summary>
    /// Manually triggers retry for the Ollama connection.
    /// </summary>
    [RelayCommand]
    public async Task RetryOllamaConnection()
    {
        await _ollamaService.Retry();

        if (_ollamaService.OllamaServiceState?.Status == ServiceStatus.Running)
        {
            await InitializeModels();
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Orchestrates the generation of the model response, handling streaming chunks,
    /// calculation of generation speed, and potential title regeneration.
    /// </summary>
    private async Task AddGeneratedMessage(Conversation conversation)
    {
        var generatedMessage = new GeneratedMessage("", 0.0);
        conversation.AddMessage(generatedMessage);

        var messageHistory = new List<Message>(conversation.Messages.ToList());
        messageHistory.RemoveAt(messageHistory.Count - 1);

        // Remove failed messages from history before generation
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

                // Regenerate title after the first 2 messages and then every 6 messages (1 & 3 exchanges)
                if ((conversation.Messages.Count - 2) % 6 == 0 || conversation.Messages.Count == 2)
                {
                    Console.WriteLine(2 % 6);
                    await RegenerateConversationTitle(conversation);
                }

                // Also regenerate if the title is still "New Conversation", but it was not updated after the first message exchange,
                // this can happen if the application is closed while a new conversation is being generated.
                // Let's just hope they didn't also switch their localization settings in the meantime :)
                if (conversation.Title == LocalizationService.GetString("NEW_CONVERSATION") &&
                    conversation.Messages.Count == 4)
                {
                    await RegenerateConversationTitle(conversation);
                }
            }
        }

        await _conversationService.InsertMessage(conversation.ConversationId, generatedMessage,
            SelectedModelName, generatedMessage.GenerationSpeed);

        conversation.Model = SelectedModelName;
    }

    /// <summary>
    /// Generates a short title for the conversation based on the message history.
    /// </summary>
    private async Task RegenerateConversationTitle(Conversation conversation)
    {
        conversation.Title = string.Empty;

        // TODO: better solution for title generation (not working for all models)
        const string request =
            "Generate only a single short title for this conversation with no use of quotation marks.";

        var tmpMessage = new Message(request);
        var messageHistory = new List<Message>(conversation.Messages.ToList()) { tmpMessage };
        await foreach (var chunk in _ollamaService.GenerateMessage(messageHistory, SelectedModelName))
        {
            if (chunk.Message != null) conversation.Title += chunk.Message.Content;
        }

        await _conversationService.UpdateConversationTitle(conversation);
    }

    /// <summary>
    /// Initializes the conversation list from the database (it's called only once).
    /// </summary>
    private async Task InitializeConversations()
    {
        Conversations = await _conversationService.GetConversations();
        if (_conversations is not { Count: > 0 })
        {
            await CreateNewConversation();
            return;
        }

        SelectedConversation = _conversations.FirstOrDefault()!;
        SelectedConversation.Messages = await _conversationService.GetMessagesForConversation(SelectedConversation);
        GetState(SelectedConversation).MessagesLoaded = true;
    }

    /// <summary>
    /// Initializes available models by querying the Ollama service (it can be called multiple times).
    /// </summary>
    private async Task InitializeModels()
    {
        // Wait for ollama to start
        await _ollamaService.WaitForRunningAsync();

        // caches the previously selected model name
        var tmpName = string.Empty;
        if (_isInitializedAsync) tmpName = SelectedModelName;

        AvailableModels.Clear();

        // check if there are available downloaded models from Ollama
        var downloadedModels = await _ollamaService.GetDownloadedModels();
        if (downloadedModels.Count == 0)
        {
            IsModelsDropdownEnabled = false;
            IsNoModelsWarningVisible = true;
            IsMessageBoxEnabled = false;
            return;
        }

        // adds downloaded models to the ComboBox
        foreach (var model in downloadedModels)
        {
            AvailableModels.Add(model.Name);
        }

        IsModelsDropdownEnabled = true;
        IsNoModelsWarningVisible = false;
        IsMessageBoxEnabled = true;

        if (!_isInitializedAsync)
        {
            // selects the first model
            SelectedModelName = AvailableModels[0];
        }
        else if (_isInitializedAsync && !string.IsNullOrEmpty(tmpName))
        {
            // sets the previously selected model
            SelectedModelName = tmpName;
        }
    }

    /// <summary>
    /// Handles changes in the Ollama service state and updates UI elements accordingly.
    /// </summary>
    private void OllamaServiceStateChanged(ServiceState? state)
    {
        if (state == null) return;
        switch (state.Status)
        {
            case ServiceStatus.Running:
                var apiHost = _configurationService.ReadSetting(ConfigurationKey.ApiHost);
                var apiPort = _configurationService.ReadSetting(ConfigurationKey.ApiPort);

                // "Remote connection" message if the app isn't connected to local ollama server
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

                if (IsRetryPanelVisible) IsRetryPanelVisible = false;
                if (IsRetryButtonVisible) IsRetryButtonVisible = false;
                if (AvailableModels.Any() && !IsModelsDropdownEnabled) IsModelsDropdownEnabled = true;
                break;

            case ServiceStatus.NotInstalled:
                _dialogService.ShowActionDialog(
                    title: LocalizationService.GetString("OLLAMA_NOT_INSTALLED"),
                    actionButtonText: LocalizationService.GetString("DOWNLOAD"),
                    action: () =>
                    {
                        RedirectToOllamaDownload();
                        // Message to AppService to shut down the app
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

            // Stopped event is raised only (as a default value) when OllamaService is initialized or the Stop() method is called
            case ServiceStatus.Stopped:
                ReplaceGeneratedMessageToFailed();
                IsRemoteConnectionTextVisible = false;
                IsRetryPanelVisible = false;
                IsRetryButtonVisible = false;
                IsMessageBoxEnabled = false;
                IsModelsDropdownEnabled = false;
                break;

            case ServiceStatus.Failed:
                // If the last message meant to be a generated one, replace it with a FailedMessage
                ReplaceGeneratedMessageToFailed();
                RetryInfoText = state.Message ?? LocalizationService.GetString("OLLAMA_CONNECTION_ERROR");
                IsRemoteConnectionTextVisible = false;
                IsRetryPanelVisible = true;
                IsRetryButtonVisible = true;
                IsMessageBoxEnabled = false;
                IsModelsDropdownEnabled = false;
                break;
        }
    }

    private void ReplaceGeneratedMessageToFailed()
    {
        // If the last message meant to be a generated one, replace it with a FailedMessage
        if (SelectedConversation is not { Messages.Count: > 0 }) return;
        if (SelectedConversation.Messages.Last() is not GeneratedMessage generatedMessage) return;
        SelectedConversation.Messages.Remove(generatedMessage);
        SelectedConversation.Messages.Add(new FailedMessage());
    }

    private void LoadSettings()
    {
        ScrollSetting = _configurationService.ReadSetting(ConfigurationKey.ScrollToBottom);
        ShowInformationalMessages =
            _configurationService.ReadSetting(ConfigurationKey.ShowInformationalMessages) == "True";
    }

    private ConversationState GetState(Conversation conversation)
    {
        return _conversationStates.GetOrCreateValue(conversation);
    }

    private static void RedirectToOllamaDownload()
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

    #endregion

    #region Helper Types

    /// <summary>
    /// Helper class to maintain transient state for conversation objects (e.g., whether messages are loaded),
    /// avoiding database queries for already loaded data.
    /// </summary>
    internal class ConversationState
    {
        public bool MessagesLoaded { get; set; }
    }

    #endregion
}
