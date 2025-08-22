﻿// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Threading.Tasks;
using avallama.Constants;
using avallama.Factories;
using avallama.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

namespace avallama.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    // PageFactory amivel elérhető az App.axaml.cs-ben létrehozott delegate, vagyis adott PageViewModel visszaadása
    private readonly PageFactory _pageFactory;
    private readonly ConfigurationService _configurationService;
    private readonly IMessenger _messenger;
    
    private string? _firstTime;
    
    [ObservableProperty] private PageViewModel? _currentPageViewModel;
    

    public MainViewModel(
        PageFactory pageFactory, 
        ConfigurationService configurationService,
        IMessenger messenger
    )
    {
        _pageFactory = pageFactory;
        _configurationService = configurationService;
        _messenger = messenger;
        
        _firstTime = _configurationService.ReadSetting(ConfigurationKey.FirstTime);
        if (string.IsNullOrEmpty(_firstTime))
        {
            CurrentPageViewModel = _pageFactory.GetPageViewModel(ApplicationPage.Greeting);
        }
        else if (_firstTime == "false")
        {
            CurrentPageViewModel = _pageFactory.GetPageViewModel(ApplicationPage.Home);
            Task.Run(async () => await CheckOllamaStart());
        }
    }
    
    

    private async Task CheckOllamaStart()
    {
        // kisebb delay, ugyanis a MainViewModel hamarabb inicializálódik mint az ApplicationService
        // és ha nincs ez a delay akkor az ApplicationService nem tudná fogadni a kérést az ollama indítás dialog megjelenítésére
        // mert az elveszne
        await Task.Delay(500);
        
        _firstTime = _configurationService.ReadSetting(ConfigurationKey.FirstTime);
        var apiHost =  _configurationService.ReadSetting(ConfigurationKey.ApiHost);
        var apiPort =  _configurationService.ReadSetting(ConfigurationKey.ApiPort);

        if (!string.IsNullOrEmpty(_firstTime) && !string.IsNullOrEmpty(apiHost) &&
            !string.IsNullOrEmpty(apiPort)) return;
        
        _configurationService.SaveSetting(ConfigurationKey.FirstTime, "false");

        // ha új felhasználó akkor AppService-t megkérjük arra hogy villantsa fel azt a mindent tudó dialogját
        _messenger.Send(new ApplicationMessage.AskOllamaStart());
    }

    [RelayCommand]
    public async Task OpenHome()
    {
        CurrentPageViewModel = _pageFactory.GetPageViewModel(ApplicationPage.Home);
        await CheckOllamaStart();
    }
    
    [RelayCommand]
    public void OpenGuide()
    {
        CurrentPageViewModel = _pageFactory.GetPageViewModel(ApplicationPage.Guide);
    }
    
    [RelayCommand]
    public void OpenModelManager()
    {
        CurrentPageViewModel = _pageFactory.GetPageViewModel(ApplicationPage.ModelManager);
    }
    
    [RelayCommand]
    public void OpenSettings()
    {
        CurrentPageViewModel = _pageFactory.GetPageViewModel(ApplicationPage.Settings);
    }
}