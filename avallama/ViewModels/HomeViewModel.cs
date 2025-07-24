// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using avallama.Constants;
using avallama.Models;
using avallama.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

namespace avallama.ViewModels;

public partial class HomeViewModel : PageViewModel
{
    private const string OllamaDownloadUrl = @"https://ollama.com/download/";

    public string LanguageLimitationWarning { get; } =
        string.Format(LocalizationService.GetString("ONLY_SUPPORTED_MODEL"), "llama3.2");

    public string ResourceLimitWarning { get; } = string.Format(LocalizationService.GetString("LOW_VRAM_WARNING"));

    public string NotDownloadedWarning { get; } =
        string.Format(LocalizationService.GetString("NOT_DOWNLOADED_WARNING"));

    private readonly OllamaService _ollamaService;
    private readonly DialogService _dialogService;
    private readonly ConfigurationService _configurationService;
    private readonly TaskCompletionSource<bool> _ollamaServerStarted = new();
    private readonly IMessenger _messenger;

    public string ScrollSetting = string.Empty;

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
        _dialogService.ShowDialog(ApplicationDialog.Settings);
    }

    [RelayCommand]
    public void OpenModelManager()
    {
        _dialogService.ShowDialog(
            ApplicationDialog.ModelManager,
            true,
            700
        );
    }

    private async Task AddGeneratedMessage()
    {
        var generatedMessage = new GeneratedMessage("", 0.0);
        SelectedConversation.AddMessage(generatedMessage);
        var messageHistory = new List<Message>(SelectedConversation.Messages.ToList());
        messageHistory.RemoveAt(messageHistory.Count - 1);

        await foreach (var chunk in _ollamaService.GenerateMessage(messageHistory))
        {
            if (chunk.Message != null) generatedMessage.Content += chunk.Message.Content;

            if (chunk.EvalCount.HasValue && chunk.EvalDuration.HasValue)
            {
                double tokensPerSecond =
                    chunk.EvalCount.GetValueOrDefault() / (double)chunk.EvalDuration * Math.Pow(10, 9);
                generatedMessage.GenerationSpeed = tokensPerSecond;
                IsWarningVisible = tokensPerSecond < 20;
            }
        }
    }

    private async Task RegenerateConversationTitle()
    {
        SelectedConversation.Title = string.Empty;
        const string request =
            "Generate only a single short title for this conversation with no use of quotation marks.";
        var tmpMessage = new Message(request);
        var messageHistory = new List<Message>(SelectedConversation.Messages.ToList()) { tmpMessage };
        await foreach (var chunk in _ollamaService.GenerateMessage(messageHistory))
        {
            if (chunk.Message != null) SelectedConversation.Title += chunk.Message.Content;
        }

        SelectedConversation.MessageCountToRegenerateTitle = 0;
    }

    private async Task GetModelInfo(string modelName)
    {
        //ezt majd jobban kéne
        AvailableModels[AvailableModels.IndexOf(modelName)] =
            modelName + await _ollamaService.GetModelParamNum(modelName);
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

                DownloadAmount =
                    $"{Math.Round((chunk.Completed.Value < 1_000_000_000 ? chunk.Completed.Value / 1_000_000m : chunk.Completed.Value / 1_000_000_000m), 2)} " +
                    $"{(chunk.Completed.Value < 1_000_000_000 ? "MB" : "GB")} / {Math.Round(chunk.Total.Value / 1_000_000_000m, 2)} GB";
            }

            if (chunk.Status != null) DownloadStatus = chunk.Status + " - " + Math.Round(DownloadProgress) + "%";
            if ((int)Math.Round(DownloadProgress) == 100) IsMaxPercent = true;
            if (chunk.Status != null && chunk.Status == "success")
                DownloadStatus = LocalizationService.GetString("VERIFYING_DOWNLOAD");
        }

        await CheckModelDownload();
        IsDownloading = false;
        await GetModelInfo(AvailableModels.FirstOrDefault() ?? "llama3.2").WaitAsync(TimeSpan.FromMilliseconds(100));
    }

    public HomeViewModel(
        OllamaService ollamaService,
        DialogService dialogService,
        ConfigurationService configurationService,
        IMessenger messenger
    )
    {
        // beállítás, hogy a viewmodel milyen paget kezel
        Page = ApplicationPage.Home;

        _dialogService = dialogService;
        _ollamaService = ollamaService;
        _ollamaService.ServiceStatusChanged += OllamaServiceStatusChanged;
        _configurationService = configurationService;
        _messenger = messenger;

        LoadSettings();

        var conversation = new Conversation(
            LocalizationService.GetString("NEW_CONVERSATION"),
            "llama3.2"
        );

        _conversations = [conversation];
        SelectedConversation = conversation;
        _availableModels = ["llama3.2", LocalizationService.GetString("LOADING_MODELS")];
        CurrentlySelectedModel = AvailableModels.LastOrDefault() ?? string.Empty;
        _ = OllamaInit();
    }

    private async Task OllamaInit()
    {
        // megvárja amíg kapcsolódik az ollama szerverhez, ez gondolom azért kell, mert hamarabb futna le ez a metódus mint hogy a szerver elindul (?)
        await _ollamaServerStarted.Task;
        await GetModelInfo(AvailableModels.FirstOrDefault() ?? "llama3.2").WaitAsync(TimeSpan.FromMilliseconds(100));
        //ezt majd dinamikusan aszerint hogy melyik modell van használatban betöltéskor
        await CheckModelDownload().WaitAsync(TimeSpan.FromMilliseconds(100));
    }

    // első inicializálásnál és beállítások mentésénél ez meghívódik, hogy pl. ne kelljen restartolni az appot
    // ha a felhasználó átváltja a görgetési beállítást, és újra betöltenie azt
    // TODO: messengerrel kell valszeg megoldani hogy settingsviewmodel értesítse homeviewmodelt
    private void LoadSettings()
    {
        ScrollSetting = _configurationService.ReadSetting("scroll-to-bottom");
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

    private void OllamaServiceStatusChanged(ServiceStatus status, string? message)
    {
        // TODO: logolni is majd
        switch (status)
        {
            case ServiceStatus.Running:
                _ollamaServerStarted.SetResult(true);
                break;
            case ServiceStatus.NotInstalled:
                _dialogService.ShowActionDialog(
                    title: LocalizationService.GetString("OLLAMA_NOT_INSTALLED"),
                    actionButtonText: LocalizationService.GetString("DOWNLOAD"),
                    action: () =>
                    {
                        RedirectToOllamaDownload();

                        // üzenet az AppServicenek hogy zárja be az appot
                        // ez azért kell mert különben ciklikus függőség alakulna ki, AppService visszatérne saját magához dependency regisztrálásnál
                        // már persze ha AppService dependencyvel oldanánk meg
                        _messenger.Send(new ShutdownMessage());
                    },
                    closeAction: () => { _messenger.Send(new ShutdownMessage()); },
                    description: LocalizationService.GetString("OLLAMA_NOT_INSTALLED_DESC")
                );
                break;
            default:
                _dialogService.ShowErrorDialog(
                    message ?? LocalizationService.GetString("OLLAMA_FAILED"),
                    true
                );
                break;
        }
    }
}