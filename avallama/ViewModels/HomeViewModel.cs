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
    
    private readonly OllamaService _ollamaService;
    
    private ObservableCollection<Message> _messages;

    public ObservableCollection<Message> Messages
    {
        get => _messages;
        set => SetProperty(ref _messages, value);
    }

    [ObservableProperty] 
    private string _newMessageText = string.Empty;
    [ObservableProperty] private bool _isWarningVisible;

    // ez async, mert nem akarjuk hogy blokkolja a főszálat
    [RelayCommand]
    private async Task SendMessage()
    {
        if (NewMessageText.Length == 0) return;
        NewMessageText = NewMessageText.Trim();
        Messages.Add(new Message(NewMessageText));
        NewMessageText = string.Empty;
        await AddGeneratedMessage();
    }

    private async Task AddGeneratedMessage()
    {
        var generatedMessage = new GeneratedMessage("", 0.0);
        Messages.Add(generatedMessage);
        List<Message> messageHistory = new List<Message>(Messages.ToList());
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
    
    public HomeViewModel(OllamaService ollamaService)
    {
        // beállítás, hogy a viewmodel milyen paget kezel
        Page = ApplicationPage.Home;
        _messages = new ObservableCollection<Message>();
        _ollamaService = ollamaService;
    }
}