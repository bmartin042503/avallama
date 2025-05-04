// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System.Diagnostics;
using System.Runtime.InteropServices;
using avallama.Constants;
using avallama.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace avallama.ViewModels;

public partial class OllamaServiceViewModel : DialogViewModel
{
    private const string OllamaDownloadUrl = @"https://ollama.com/download/";
    private readonly OllamaService _ollamaService;
    private readonly DialogService _dialogService;
    private readonly AppLauncherService _appLauncherService;

    [ObservableProperty] private string _ollamaServiceStatusText;
    [ObservableProperty] private bool _ollamaServiceRunning;
    [ObservableProperty] private bool _ollamaServiceLoading;
    [ObservableProperty] private string _downloadButtonText = string.Empty;
    [ObservableProperty] private bool _isDownloadButtonVisible;
    [ObservableProperty] private bool _isCloseButtonVisible;
    
    
    public OllamaServiceViewModel(OllamaService ollamaService, DialogService dialogService, AppLauncherService appLauncherService)
    {
        DialogType = ApplicationDialog.OllamaService;
        _dialogService = dialogService;
        _appLauncherService = appLauncherService;
        _ollamaServiceStatusText = LocalizationService.GetString("AVALLAMA_STARTING");
        _ollamaService = ollamaService;
        _ollamaService.ServiceStatusChanged += OllamaServiceStatusChanged;
        OllamaServiceLoading = true;
    }
    
    // a metódus ami feliratkozik az OllamaServiceben lévő eventre, és ha az event meghívódik akkor ez elkapja
    // és felfalja
    private void OllamaServiceStatusChanged(ServiceStatus status, string? message)
    {
        if(message != null) OllamaServiceStatusText = message;
        switch (status)
        {
            case ServiceStatus.Running:
                OllamaServiceRunning = true;
                _dialogService.CloseDialog(ApplicationDialog.OllamaService);
                _appLauncherService.InitializeMainWindow();
                break;
            case ServiceStatus.NotInstalled:
                DownloadButtonText = string.Format(LocalizationService.GetString("DOWNLOAD_OLLAMA"),
                    RuntimeInformation.RuntimeIdentifier);
                IsDownloadButtonVisible = true;
                IsCloseButtonVisible = true;
                break;
            case ServiceStatus.Failed:
                IsCloseButtonVisible = true;
                break;
        }
        OllamaServiceLoading = false;
    }
    
    [RelayCommand]
    public void RedirectToOllamaDownload()
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

    [RelayCommand]
    public void Close()
    {
        _appLauncherService.CloseApplication();
    }
}