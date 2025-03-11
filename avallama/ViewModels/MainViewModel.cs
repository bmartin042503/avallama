// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System.Runtime.InteropServices;
using avallama.Constants;
using avallama.Factories;
using avallama.Services;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace avallama.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    // PageFactory amivel elérhető az App.axaml.cs-ben létrehozott delegate, vagyis adott PageViewModel visszaadása
    private readonly PageFactory _pageFactory;
    
    [ObservableProperty] private PageViewModel _currentPageViewModel;
    [ObservableProperty] private bool _ollamaServiceRunning;
    [ObservableProperty] private bool _ollamaServiceLoading;
    [ObservableProperty] private string? _ollamaServiceStatusText;
    [ObservableProperty] private string? _downloadButtonText;
    [ObservableProperty] private bool _isDownloadButtonVisible;
    
    private OllamaService _ollamaService;

    public MainViewModel(PageFactory pageFactory, OllamaService ollamaService)
    {
        _pageFactory = pageFactory;
        CurrentPageViewModel = _pageFactory.GetPageViewModel(ApplicationPage.Greeting);
        
        _ollamaService = ollamaService;
        _ollamaService.ServiceStatusChanged += OllamaServiceStatusChanged;
        OllamaServiceStatusText = LocalizationService.GetString("OLLAMA_STARTING");
        IsDownloadButtonVisible = false;
        OllamaServiceLoading = true;
    }

    [RelayCommand]
    private void GoToHome()
    {
        CurrentPageViewModel = _pageFactory.GetPageViewModel(ApplicationPage.Home);
    }
    
    // ezt lehet majd használni viewban commandként a retry gombra
    [RelayCommand]
    public void RetryOllamaService()
    {
        // TODO: Start() metódus
    }

    // a metódus ami feliratkozik az OllamaServiceben lévő eventre, és ha az event meghívódik akkor ez elkapja
    // és felfalja
    private void OllamaServiceStatusChanged(ServiceStatus status, string? message)
    {
        if(message != null) OllamaServiceStatusText = message;
        OllamaServiceLoading = false;
        if (status == ServiceStatus.Running)
        {
            OllamaServiceRunning = true;
        }
        else if (status == ServiceStatus.NotInstalled)
        {
            DownloadButtonText = string.Format(LocalizationService.GetString("DOWNLOAD_OLLAMA"),
                RuntimeInformation.RuntimeIdentifier);
            IsDownloadButtonVisible = true;
        }
    }
    
}