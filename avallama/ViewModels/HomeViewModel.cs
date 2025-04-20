// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using avallama.Constants;
using avallama.Models;
using avallama.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace avallama.ViewModels;

public partial class HomeViewModel : PageViewModel
{
    public string LanguageLimitationWarning { get; } = String.Format(LocalizationService.GetString("ONLY_SUPPORTED_MODEL"), "llama3.2");
    public string ResourceLimitWarning { get; } = String.Format(LocalizationService.GetString("LOW_VRAM_WARNING"));
    public string NotDownloadedWarning { get; } = String.Format(LocalizationService.GetString("NOT_DOWNLOADED_WARNING"));
    
    private readonly OllamaService _ollamaService;
    private readonly DialogService _dialogService;

    private ObservableCollection<Conversation> _conversations;
    
    public ObservableCollection<Conversation> Conversations
    {
        get => _conversations;
        set => SetProperty(ref _conversations, value);
    }
    
    private ObservableCollection<string> _availableModels;
    
    public ObservableCollection<string> AvailableModels
    {
        get => _availableModels;
        set => SetProperty(ref _availableModels, value);
    }

    [ObservableProperty] private string _newMessageText = string.Empty;
    [ObservableProperty] private bool _isWarningVisible;
    [ObservableProperty] private bool _isNotDownloadedVisible;
    [ObservableProperty] private bool _isDownloaded;
    [ObservableProperty] private string _currentlySelectedModel;
    [ObservableProperty] private bool _isDownloading;
    [ObservableProperty] private string _downloadStatus = "";
    [ObservableProperty] private double _downloadProgress;
    [ObservableProperty] private bool _isMaxPercent;
    [ObservableProperty] private string _downloadSpeed = string.Empty;
    [ObservableProperty] private string _downloadAmount = string.Empty;
    [ObservableProperty] private Conversation _selectedConversation;
    
    [RelayCommand]
    private async Task SendMessage()
    {
        if (NewMessageText.Length == 0) return;
        NewMessageText = NewMessageText.Trim();
        SelectedConversation.AddMessage(new Message(NewMessageText));
        SelectedConversation.MessageCountToRegenerateTitle++;
        NewMessageText = string.Empty;
        await AddGeneratedMessage();
        if (SelectedConversation.MessageCountToRegenerateTitle == 3)
        {
            await RegenerateConversationTitle();
        }
    }

    [RelayCommand]
    public void OpenSettings()
    {
        _dialogService.ShowDialog(ApplicationDialogContent.Settings);
    }

    private async Task AddGeneratedMessage()
    {
        var generatedMessage = new GeneratedMessage("", 0.0);
        SelectedConversation.AddMessage(generatedMessage);
        var messageHistory = new List<Message>(SelectedConversation.Messages.ToList());
        messageHistory.RemoveAt(messageHistory.Count - 1);

        await foreach (var chunk in _ollamaService.GenerateMessage(messageHistory))
        {
            if(chunk.Message != null) generatedMessage.Content += chunk.Message.Content;
            
            if(chunk.EvalCount.HasValue && chunk.EvalDuration.HasValue)
            {
                double tokensPerSecond = chunk.EvalCount.GetValueOrDefault() / (double)chunk.EvalDuration * Math.Pow(10,9);
                generatedMessage.GenerationSpeed = tokensPerSecond;
                IsWarningVisible = tokensPerSecond < 20;
            }
        }
    }

    private async Task RegenerateConversationTitle()
    {
        SelectedConversation.Title = string.Empty;
        const string request = "Generate just a single short title for this conversation with no use of \"";
        var tmpMessage = new Message(request);
        var messageHistory = new List<Message>(SelectedConversation.Messages.ToList()) { tmpMessage };
        await foreach (var chunk in _ollamaService.GenerateMessage(messageHistory))
        {
            if(chunk.Message != null) SelectedConversation.Title += chunk.Message.Content;
        }
        SelectedConversation.MessageCountToRegenerateTitle = 0;
    }

    private async Task GetModelInfo(string modelName)
    {
        //ezt majd jobban kéne
        AvailableModels[AvailableModels.IndexOf(modelName)] = modelName + await _ollamaService.GetModelParamNum(modelName);
        CurrentlySelectedModel = AvailableModels.FirstOrDefault() ?? modelName;
    }

    private async Task CheckModelDownload()
    {
        IsDownloaded = await _ollamaService.IsModelDownloaded();
        IsNotDownloadedVisible = !IsDownloaded;
    }

    public async Task DownloadModel()
    {
        IsDownloading = true;
        DownloadStatus = LocalizationService.GetString("STARTING_DOWNLOAD");
        
        var speedCalculator = new NetworkSpeedCalculator();
        
        await foreach (var chunk in _ollamaService.PullModel("llama3.2"))
        {
            if (chunk.Total.HasValue && chunk.Completed.HasValue)
            {
                DownloadProgress = (double)chunk.Completed.Value / chunk.Total.Value * 100;
                var speed = speedCalculator.CalculateSpeed(chunk.Completed.Value);
                if (speed > 0)
                {
                    double bytesRemaining = chunk.Total.Value - chunk.Completed.Value;
                    var minutes = (int)(bytesRemaining / (speed * 1_000_000 / 8) / 60);
                    var seconds = (int)(bytesRemaining / (speed * 1_000_000 / 8) % 60);
                    DownloadSpeed = Math.Round(speed, 2) + " Mbps - " + $"{minutes:D2}:{seconds:D2}";
                }
                DownloadAmount = $"{Math.Round((chunk.Completed.Value < 1_000_000_000 ? chunk.Completed.Value / 1_000_000m : chunk.Completed.Value / 1_000_000_000m), 2)} " +
                                 $"{(chunk.Completed.Value < 1_000_000_000 ? "MB" : "GB")} / {Math.Round(chunk.Total.Value / 1_000_000_000m, 2)} GB";
            }
            if(chunk.Status != null) DownloadStatus = chunk.Status + " - " + Math.Round(DownloadProgress) + "%";
            if((int)Math.Round(DownloadProgress) == 100) IsMaxPercent = true;
            if(chunk.Status != null && chunk.Status == "success") DownloadStatus = LocalizationService.GetString("VERIFYING_DOWNLOAD");
        }

        await CheckModelDownload();
        IsDownloading = false;
    }
    
    public HomeViewModel(OllamaService ollamaService, DialogService dialogService)
    {
        // beállítás, hogy a viewmodel milyen paget kezel
        Page = ApplicationPage.Home;
        
        _dialogService = dialogService;

        var conversation = new Conversation(
            LocalizationService.GetString("NEW_CONVERSATION"),
            "llama3.2"
        );
        
        _conversations = [conversation];
        SelectedConversation = conversation;
        _availableModels = ["llama3.2", LocalizationService.GetString("LOADING_MODELS")];
        _ollamaService = ollamaService;
        
        CurrentlySelectedModel = AvailableModels.LastOrDefault() ?? string.Empty;
        
        GetModelInfo(AvailableModels.FirstOrDefault() ?? "llama3.2").WaitAsync(TimeSpan.FromMilliseconds(100));
        //ezt majd dinamikusan aszerint hogy melyik modell van használatban betöltéskor
        CheckModelDownload().WaitAsync(TimeSpan.FromMilliseconds(100));
    }
}