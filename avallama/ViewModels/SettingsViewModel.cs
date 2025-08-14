// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Diagnostics;
using System.Net;
using avallama.Constants;
using avallama.Services;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

namespace avallama.ViewModels;

public partial class SettingsViewModel : PageViewModel
{
    private readonly DialogService _dialogService;
    private readonly ConfigurationService _configurationService;
    private readonly IMessenger _messenger;
    private const string Url = @"https://github.com/4foureyes/avallama/";

    private int _selectedLanguageIndex;
    private int _selectedThemeIndex;
    private int _selectedScrollIndex;
    private int _defaultLanguageIndex;
    private string _apiHost = "localhost";
    private int _apiPort = 11434;
    private bool _changesTextVisibility;
    private bool _restartNeeded;

    // OnPropertyChanged metódusokkal most ObservableProperty helyett, csak hogy kezelni lehessen a set-et
    public bool RestartNeeded
    {
        get => _restartNeeded;
        set
        {
            _restartNeeded = value;
            OnPropertyChanged();
        }
    }

    public int SelectedLanguageIndex
    {
        get => _selectedLanguageIndex;
        set
        {
            _selectedLanguageIndex = value;
            RestartNeeded = _defaultLanguageIndex != value;
            OnPropertyChanged();
        }
    }

    public int SelectedThemeIndex
    {
        get => _selectedThemeIndex;
        set
        {
            _selectedThemeIndex = value;
            OnPropertyChanged();
        }
    }

    public int SelectedScrollIndex
    {
        get => _selectedScrollIndex;
        set
        {
            _selectedScrollIndex = value;
            OnPropertyChanged();
        }
    }

    public bool ChangesTextVisibility
    {
        get => _changesTextVisibility;
        set
        {
            _changesTextVisibility = value;
            OnPropertyChanged();
        }
    }
    
    public string ApiHost
    {
        get => _apiHost;
        set
        {
            _apiHost = value;
            OnPropertyChanged();
        }
    }

    public int ApiPort
    {
        get => _apiPort;
        set
        {
            _apiPort = value;
            OnPropertyChanged();
        }
    }
    
    public SettingsViewModel(DialogService dialogService, ConfigurationService configurationService,  IMessenger messenger)
    {
        Page = ApplicationPage.Settings;
        _dialogService = dialogService;
        _configurationService = configurationService;
        _messenger = messenger;
        ChangesTextVisibility = false;
        LoadSettings();
    }

    private void LoadSettings()
    {
        var language = _configurationService.ReadSetting(ConfigurationKey.Language);
        SelectedLanguageIndex = language switch
        {
            "hungarian" => 0,
            _ => 1
        };
        
        _defaultLanguageIndex = SelectedLanguageIndex;
        RestartNeeded = false;
        
        var theme = _configurationService.ReadSetting(ConfigurationKey.ColorScheme);
        SelectedThemeIndex = theme switch
        {
            "light" => 0,
            _ => 1
        };

        var scrollToBottom = _configurationService.ReadSetting(ConfigurationKey.ScrollToBottom);
        SelectedScrollIndex = scrollToBottom switch
        {
            "auto" => 0,
            "float" => 1,
            "none" => 2,
            _ => 1
        };
        
        var hostSetting = _configurationService.ReadSetting(ConfigurationKey.ApiHost);
        ApiHost = string.IsNullOrEmpty(hostSetting) ? "localhost" : hostSetting;
        var portString = _configurationService.ReadSetting(ConfigurationKey.ApiPort);
        ApiPort = int.TryParse(portString, out var parsedPort) ? parsedPort : 11434;
    }

    private void SaveSettings()
    {
        var colorScheme = SelectedThemeIndex switch
        {
            0 => "light",
            1 => "dark",
            _ => string.Empty
        };

        var language = SelectedLanguageIndex switch
        {
            0 => "hungarian",
            1 => "english",
            _ => string.Empty
        };

        var scrollToBottom = SelectedScrollIndex switch
        {
            0 => "auto",
            1 => "float",
            2 => "none",
            _ => "float"
        };
        _configurationService.SaveSetting(ConfigurationKey.ColorScheme, colorScheme);
        _configurationService.SaveSetting(ConfigurationKey.Language, language);
        _configurationService.SaveSetting(ConfigurationKey.ScrollToBottom, scrollToBottom);
        ChangesTextVisibility = true;

        if (!IsValidHost(ApiHost))
        {
            _dialogService.ShowErrorDialog(LocalizationService.GetString("INVALID_HOST_ERR"));
            ApiHost = _configurationService.ReadSetting(ConfigurationKey.ApiHost);
            return;
        }
        
        if (!IsValidPort(ApiPort.ToString()))
        {
            _dialogService.ShowErrorDialog(LocalizationService.GetString("INVALID_PORT_ERR"));
            ApiPort = int.Parse(_configurationService.ReadSetting(ConfigurationKey.ApiPort));
            return;
        }
        
        _configurationService.SaveSetting(ConfigurationKey.ApiHost, ApiHost);
        _configurationService.SaveSetting(ConfigurationKey.ApiPort, ApiPort.ToString());
        _messenger.Send(new ReloadSettingsMessage());
    }

    [RelayCommand]
    public void OnHyperlinkClicked()
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = Url,
            UseShellExecute = true
        });
    }

    [RelayCommand]
    public void Save()
    {
        SaveSettings();
    }
    
    private bool IsValidHost(string host)
    {
        return host == "localhost" || IPAddress.TryParse(host, out _);
    }

    private bool IsValidPort(string port)
    {
        return int.TryParse(port, out var result) && result > 0 && result < 65536;
    }
}