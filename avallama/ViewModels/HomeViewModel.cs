using System;
using System.Collections.ObjectModel;
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
    private readonly PerformanceService _performanceService = new PerformanceService();
    
    private ObservableCollection<Message> _messages;

    public ObservableCollection<Message> Messages
    {
        get => _messages;
        set => SetProperty(ref _messages, value);
    }

    [ObservableProperty] 
    private string _newMessageText = string.Empty;

    [ObservableProperty] private string _cpuUsage;
    [ObservableProperty] private string _ramUsage;
    [ObservableProperty] private string _gpuUsage;
    [ObservableProperty] private bool _isWarningVisible;

    // ez async, mert nem akarjuk hogy blokkolja a főszálat
    [RelayCommand]
    private async Task SendMessage()
    {
        /* tesztre */
        // Messages.Add(new Message("tesztüzenet"));
        // Messages.Add(new GeneratedMessage("Lorem ipsum dolor sit amet. Teszt szöveg, teszt szöveg. Tesztelem a kijelölést", 10.0));
        
        if (NewMessageText.Length == 0) return;
        NewMessageText = NewMessageText.Trim();
        Messages.Add(new Message(NewMessageText));
        var tmp = NewMessageText;
        NewMessageText = string.Empty;
        await AddGeneratedMessage(tmp);
    }

    private async Task AddGeneratedMessage(string prompt)
    {
        var generatedMessage = new GeneratedMessage("", 0.0);
        Messages.Add(generatedMessage);

        await foreach (var chunk in _ollamaService.GenerateMessage(prompt))
        {
            generatedMessage.Content += chunk.Response;
            
            if(chunk.EvalCount.HasValue && chunk.EvalDuration.HasValue)
            {
                double tokensPerSecond = chunk.EvalCount.GetValueOrDefault() / (double)chunk.EvalDuration * Math.Pow(10,9);
                generatedMessage.GenerationSpeed = tokensPerSecond;
                IsWarningVisible = tokensPerSecond < 20;
            }
        }
    }

    private async Task PollPerformance()
    {
        while (true)
        {
            await Task.Delay(50);
            var cpu = await _performanceService.CalculateCpuUsage();
            CpuUsage = cpu + "% CPU";
            var ram = _performanceService.CalculateMemoryUsage();
            RamUsage = ram + "% RAM";
            var gpu = _performanceService.GetTotalGpuUsageWindows();
            GpuUsage = gpu + "% GPU";
        }
    }
    
    public HomeViewModel(OllamaService ollamaService)
    {
        // beállítás, hogy a viewmodel milyen paget kezel
        Page = ApplicationPage.Home;
        _messages = new ObservableCollection<Message>();
        _ollamaService = ollamaService;
        Task.Run(PollPerformance);
    }
}