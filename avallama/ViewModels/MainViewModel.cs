// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Configuration;
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
    private readonly string _firstTime;
    private readonly ConfigurationService _configurationService;
    
    [ObservableProperty] private PageViewModel? _currentPageViewModel;
    

    public MainViewModel(PageFactory pageFactory, ConfigurationService configurationService)
    {
        _pageFactory = pageFactory;
        _configurationService = configurationService;
        _firstTime = _configurationService.ReadSetting("first-time");
        if (string.IsNullOrEmpty(_firstTime))
        {
            CurrentPageViewModel = _pageFactory.GetPageViewModel(ApplicationPage.Greeting);
        }
        else if (_firstTime == "false")
        {
            CurrentPageViewModel = _pageFactory.GetPageViewModel(ApplicationPage.Home);
        }
    }

    [RelayCommand]
    public void GoToHome()
    {
        if (string.IsNullOrEmpty(_firstTime))
        {
            _configurationService.SaveSetting("first-time", "false");
        }
        CurrentPageViewModel = _pageFactory.GetPageViewModel(ApplicationPage.Home);
    }
    
    [RelayCommand]
    private void GoToGuide()
    {
        CurrentPageViewModel = _pageFactory.GetPageViewModel(ApplicationPage.Guide);
    }
    
    // ezt lehet majd használni viewban commandként a retry gombra
    [RelayCommand]
    public void RetryOllamaService()
    {
        // TODO: Start() metódus
    }
    
}