using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using avallama.Constants;
using avallama.Models;
using avallama.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace avallama.ViewModels;

public partial class HomeViewModel : PageViewModel
{
    public string LanguageLimitationWarning { get; } = String.Format(LocalizationService.GetString("ONLY_SUPPORTED_MODEL"), "llama3.2");
    public string ResourceLimitWarning { get; } = String.Format(LocalizationService.GetString("LOW_VRAM_WARNING"), 69, 420);
    
    private ObservableCollection<Message> _messages;

    public ObservableCollection<Message> Messages
    {
        get => _messages;
        set => SetProperty(ref _messages, value);
    }

    [ObservableProperty] 
    private string _newMessageText = string.Empty;

    private static string[] _testMessages =
    [
        "Hello World!",
        "Szia, ez egy automatikus válasz az avallamától!",
        "NA MIVAN ÖCSÉM",
        "GYÁÁÁÁ NE ÍROGASSÁ MÁÁÁÁÁ",
        "Sajtoskalács",
        "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Suspendisse aliquam ipsum ut pulvinar convallis. Cras tempus feugiat erat, in tincidunt sem suscipit vitae",
        "bro használd a chatgpt, hát nem látod hogy még nem vagyok implementálva?"
    ];

    [RelayCommand]
    private void SendMessage()
    {
        if (NewMessageText.Length == 0) return;
        NewMessageText = NewMessageText.Trim();
        var rnd = new Random();
        Messages.Add(new Message(NewMessageText));

        var gm = new GeneratedMessage(_testMessages[rnd.Next(7)])
        {
            GenerationSpeed = rnd.Next(10, 60)
        };
        Messages.Add(gm);
        NewMessageText = string.Empty;
    } 
    
    public HomeViewModel()
    {
        // beállítás, hogy a viewmodel milyen paget kezel
        Page = ApplicationPage.Home;
        
        _messages = new ObservableCollection<Message>();
    }
}