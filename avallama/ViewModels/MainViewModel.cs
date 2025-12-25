// Copyright (c) Márk Csörgő and Martin Bartos
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
    // PageFactory which can reach the delegate created in App.axaml.cs, i.e. returns the given PageViewModel
    private readonly PageFactory _pageFactory;
    private readonly ConfigurationService _configurationService;
    private readonly IModelCacheService _modelCacheService;
    private readonly IMessenger _messenger;

    private string? _firstTime;

    [ObservableProperty] private PageViewModel? _currentPageViewModel;

    public MainViewModel(
        PageFactory pageFactory,
        ConfigurationService configurationService,
        IModelCacheService modelCacheService,
        IMessenger messenger
    )
    {
        _pageFactory = pageFactory;
        _configurationService = configurationService;
        _modelCacheService = modelCacheService;
        _messenger = messenger;

        _messenger.Register<ApplicationMessage.RequestPage>(this, (_, msg) =>
        {
            CurrentPageViewModel = _pageFactory.GetPageViewModel(msg.Page);
        });

        _firstTime = _configurationService.ReadSetting(ConfigurationKey.FirstTime);
        if (string.IsNullOrEmpty(_firstTime))
        {
            CurrentPageViewModel = _pageFactory.GetPageViewModel(ApplicationPage.Greeting);
        }
        else if (_firstTime == "false")
        {
            CurrentPageViewModel = _pageFactory.GetPageViewModel(ApplicationPage.Home);
            CheckOllamaStart();
        }
    }

    private void CheckOllamaStart()
    {
        _firstTime = _configurationService.ReadSetting(ConfigurationKey.FirstTime);
        var apiHost = _configurationService.ReadSetting(ConfigurationKey.ApiHost);
        var apiPort = _configurationService.ReadSetting(ConfigurationKey.ApiPort);

        if (!string.IsNullOrEmpty(_firstTime) && !string.IsNullOrEmpty(apiHost) &&
            !string.IsNullOrEmpty(apiPort)) return;

        _configurationService.SaveSetting(ConfigurationKey.FirstTime, "false");

        // if the user is new, we request the AppService to show its all-knowing dialog
        _messenger.Send(new ApplicationMessage.AskOllamaStart());
    }

    [RelayCommand]
    public void OpenHome()
    {
        CheckOllamaStart();
        CurrentPageViewModel = _pageFactory.GetPageViewModel(ApplicationPage.Home);
    }

    [RelayCommand]
    public void OpenGuide()
    {
        CurrentPageViewModel = _pageFactory.GetPageViewModel(ApplicationPage.Guide);
    }

    [RelayCommand]
    public async Task OpenModelManager()
    {
        CurrentPageViewModel = _pageFactory.GetPageViewModel(ApplicationPage.ModelManager);
        var models = await _modelCacheService.GetCachedModelsAsync();
        if (models.Count == 0)
        {
            CurrentPageViewModel = _pageFactory.GetPageViewModel(ApplicationPage.Scraper);
        }
    }

    [RelayCommand]
    public void OpenSettings()
    {
        CurrentPageViewModel = _pageFactory.GetPageViewModel(ApplicationPage.Settings);
    }

    [RelayCommand]
    public void StartScraper()
    {
        CurrentPageViewModel = _pageFactory.GetPageViewModel(ApplicationPage.Scraper);
    }
}
