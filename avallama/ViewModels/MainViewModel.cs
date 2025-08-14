// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

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
    private readonly string? _firstTime;
    private readonly ConfigurationService _configurationService;
    private readonly IMessenger _messenger;
    
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
        }
    }

    [RelayCommand]
    public void OpenHome()
    {
        if (string.IsNullOrEmpty(_firstTime))
        {
            _configurationService.SaveSetting(ConfigurationKey.FirstTime, "false");
            
            // ha új felhasználó akkor AppService-t megkérjük arra hogy villantsa fel azt a mindent tudó dialogját
            _messenger.Send(new CheckOllamaStartMessage());
        }
        CurrentPageViewModel = _pageFactory.GetPageViewModel(ApplicationPage.Home);
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