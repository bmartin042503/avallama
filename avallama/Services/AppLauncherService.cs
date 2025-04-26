// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using avallama.Constants;
using avallama.ViewModels;
using avallama.Views;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;

namespace avallama.Services;

// segéd osztály az alkalmazás indításának személyre szabásához
public class AppLauncherService
{
    private bool _isMainWindowInitialized;
    private readonly MainViewModel _mainViewModel;
    private readonly DialogService _dialogService;
    
    public AppLauncherService(DialogService dialogService, MainViewModel mainViewModel)
    {
        _mainViewModel = mainViewModel;
        _dialogService = dialogService;
    }

    public void Run()
    {
        _dialogService.ShowDialog(ApplicationDialog.OllamaService);
    }

    public void CloseApplication()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop) return;
        desktop.Shutdown();
    }

    public void InitializeMainWindow()
    {
        if (_isMainWindowInitialized) return;
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop) return;
        desktop.MainWindow = new MainWindow
        {
            DataContext = _mainViewModel
        };
        desktop.MainWindow.Show();
        _isMainWindowInitialized = true;
    }
}