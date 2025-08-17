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
using avallama.Utilities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace avallama.ViewModels;

public class ReloadSettingsMessage() : ValueChangedMessage<bool>(true);

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
    private readonly DatabaseService _databaseService;

    private readonly TaskCompletionSource<bool> _ollamaServerStarted = new();
    private readonly IMessenger _messenger;

    public string ScrollSetting = string.Empty;

    private ObservableStack<Conversation> _conversations;
    public ObservableStack<Conversation> Conversations
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
    
    // TODO:
    // Van egy bug amit kicsit nehezen lehet reprodukálni, de épp a 3. üzenetem írtam, generálta volna az új beszélgetés címet az ollama
    // majd átkattintottam gyors egy másik beszélgetésbe és annak a címébe vitte bele az új címet, hozzáfűzte
    // és az előző beszélgetés címe eltűnt, nem is tudtam belekattintani, ezt meg lehet csinálni többször, átviszi a generálást máshova
    // ezt majd vhogy javítani
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
    public async Task CreateNewConversation()
    {
        var newConversation = new Conversation(
            LocalizationService.GetString("NEW_CONVERSATION"),
            "llama3.2"
        )
        {
            ConversationId = await _databaseService.CreateConversation()
        };
        Conversations.Push(newConversation);
        SelectedConversation = newConversation;
    }

    [RelayCommand]
    public void SelectConversation(object parameter)
    {
        if (parameter is Guid guid)
        {
            if (guid == SelectedConversation.ConversationId) return;
            var selectedConversation = Conversations.FirstOrDefault(x => x.ConversationId == guid);
            if (selectedConversation == null) return;
            SelectedConversation = selectedConversation;
        }
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
        DatabaseService databaseService,
        IMessenger messenger
    )
    {
        // beállítás, hogy a viewmodel milyen paget kezel
        Page = ApplicationPage.Home;

        _dialogService = dialogService;
        _ollamaService = ollamaService;
        _ollamaService.ServiceStatusChanged += OllamaServiceStatusChanged;
        _configurationService = configurationService;
        _databaseService = databaseService;
        _messenger = messenger;
        
        _messenger.Register<ReloadSettingsMessage>(this, (_, _) => { LoadSettings(); });
        
        LoadSettings();

        // TODO: ezt majd lecserélni úgy hogy a db-ből jöjjön
        var conversation = new Conversation(
            LocalizationService.GetString("NEW_CONVERSATION"),
            "llama3.2"
        )
        {
            ConversationId = Guid.NewGuid()
        };

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
        
        await GetModelInfo(AvailableModels.FirstOrDefault() ?? "llama3.2");
        
        //ezt majd dinamikusan aszerint hogy melyik modell van használatban betöltéskor
        await CheckModelDownload();
    }
    
    private void LoadSettings()
    {
        ScrollSetting = _configurationService.ReadSetting(ConfigurationKey.ScrollToBottom);
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