using System;
using System.Runtime.InteropServices;
using avallama.Constants;
using avallama.Factories;
using avallama.Services;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

namespace avallama.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    // PageFactory amivel elérhető az App.axaml.cs-ben létrehozott delegate, vagyis adott PageViewModel visszaadása
    private readonly PageFactory _pageFactory;

    public bool IsWindows { get; } = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    
    [ObservableProperty]
    private PageViewModel _currentPageViewModel;

    [ObservableProperty] 
    private bool _ollamaProcessRunning;

    [ObservableProperty] 
    private bool _ollamaProcessLoading;
    
    [ObservableProperty]
    private string? _ollamaProcessMessage;
    
    [ObservableProperty]
    private SolidColorBrush _processTextColor;

    public MainViewModel(PageFactory pageFactory, IMessenger messenger)
    {
        _pageFactory = pageFactory;
        OllamaProcessMessage = LocalizationService.GetString("PROCESS_STARTING");
        OllamaProcessLoading = true;
        ProcessTextColor = new SolidColorBrush(Colors.Black);
        messenger.Register<OllamaProcessInfo>(this, (recipient, processInfo) =>
        {
            if (processInfo.Status == ProcessStatus.Failed)
            {
                OllamaProcessMessage = String.Format(LocalizationService.GetString("PROCESS_FAILED"), processInfo.Message);
                OllamaProcessRunning = false;
                OllamaProcessLoading = false;
                ProcessTextColor = new SolidColorBrush(Colors.Red);
            }
            else if(processInfo.Status == ProcessStatus.Running)
            {
                OllamaProcessMessage = LocalizationService.GetString("PROCESS_STARTED");
                OllamaProcessLoading = false;
                OllamaProcessRunning = true;
                ProcessTextColor = new SolidColorBrush(Colors.Green);
            }
        });
        CurrentPageViewModel = _pageFactory.GetPageViewModel(ApplicationPage.Greeting);
    }

    [RelayCommand]
    private void GoToHome()
    {
        CurrentPageViewModel = _pageFactory.GetPageViewModel(ApplicationPage.Home);
    }
}