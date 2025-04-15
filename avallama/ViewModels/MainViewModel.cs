// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
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
    
    [ObservableProperty] private PageViewModel? _currentPageViewModel;
    

    public MainViewModel(PageFactory pageFactory, ConfigurationService configurationService)
    {
        _pageFactory = pageFactory;
        
        var firstTime = configurationService.ReadSetting("first-time");
        if (string.IsNullOrEmpty(firstTime))
        {
            CurrentPageViewModel = _pageFactory.GetPageViewModel(ApplicationPage.Greeting);
            configurationService.SaveSetting("first-time", "false");
        }
        else if (firstTime == "false")
        {
            CurrentPageViewModel = _pageFactory.GetPageViewModel(ApplicationPage.Home);
        }
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
    
}