// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System.Collections.ObjectModel;
using System.Threading.Tasks;
using avallama.Constants.States;
using avallama.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace avallama.ViewModels;

public partial class ConversationViewModel : ViewModelBase
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsInitializing))]
    private Conversation _conversation = new ("", "");

    [ObservableProperty] private string _selectedModelName = string.Empty;

    // TODO: pass HomeVM's references to properties upon initializing ConversationVM in HomeVM
    // so each ConversationVM will have the same values, HomeVM is the single source of truth

    [ObservableProperty] private bool _showInformationalMessages;
    [ObservableProperty] private bool _isRemoteTextVisible;
    [ObservableProperty] private bool _isRunningSlowTextVisible;
    [ObservableProperty] private bool _isNoModelsTextVisible;
    [ObservableProperty] private string _remoteText = string.Empty;
    [ObservableProperty] private string _runningSlowText = string.Empty;
    [ObservableProperty] private string _noModelsText = string.Empty;
    [ObservableProperty] private string _newMessageText = string.Empty;

    public ObservableCollection<string> AvailableModels { get; } = [];

    public bool IsInitializing => Conversation.Status.ConversationState == ConversationState.Initializing;

    // TODO: extract logic from HomeViewModel here and implement missing functionality

    /// <summary>
    /// Deletes the specified message.
    /// </summary>
    /// <param name="parameter">Message to delete</param>
    [RelayCommand]
    public async Task DeleteMessageAsync(object parameter)
    {
        // TODO: implement
    }


    /// <summary>
    /// Sends the current message text to the conversation, saves it to the database,
    /// and triggers the model response generation.
    /// </summary>
    [RelayCommand]
    private async Task SendMessageAsync()
    {
        // TODO: implement
    }
}
