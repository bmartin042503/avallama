using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
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
        PostMessage(NewMessageText);
        NewMessageText = string.Empty;
    }

    private async void PostMessage(string prompt)
    {
        const string url = "http://localhost:11434/api/generate";
        
        var data = new
        {
            model = "llama3.2",
            prompt = prompt,
            stream = false
        };
        
        using (var client = new HttpClient())
        {
            var jsonData = JsonSerializer.Serialize(data);
            var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

            try
            {
                HttpResponseMessage response = await client.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    var responseString = await response.Content.ReadAsStringAsync();
                    var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseString);

                    if (jsonResponse.TryGetProperty("response", out var answer) && 
                        jsonResponse.TryGetProperty("eval_count", out var evalCount) &&
                        jsonResponse.TryGetProperty("eval_duration", out var evalDuration))
                    {
                        AddMessage(answer.GetString(), (double)evalCount.GetInt32()/evalDuration.GetInt64() * 1e9);
                    }
                }
                else
                {
                    AddMessage("An error occured, please restart the application. Error message: " + response.StatusCode, 0);
                }
            }
            catch (Exception ex)
            {
                AddMessage("Exception occured, please restart the application: " + ex.Message, 0);
            }
        }
    }

    private void AddMessage(string message, double speed)
    {
        var gm = new GeneratedMessage(message)
        {
            GenerationSpeed = Math.Round(speed, 2)
        };
        Messages.Add(gm);
    }
    
    public HomeViewModel()
    {
        // beállítás, hogy a viewmodel milyen paget kezel
        Page = ApplicationPage.Home;
        
        _messages = new ObservableCollection<Message>();
    }
}