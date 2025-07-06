// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Diagnostics;
using System.Net;
using avallama.Constants;
using avallama.Services;
using CommunityToolkit.Mvvm.Input;

namespace avallama.ViewModels;

public partial class SettingsViewModel : DialogViewModel
{
    private readonly DialogService _dialogService;
    private readonly ConfigurationService _configurationService;
    private const string Url = @"https://github.com/4foureyes/avallama/";

    private int _selectedLanguageIndex;
    private int _selectedThemeIndex;
    private int _selectedScrollIndex;
    private int _defaultLanguageIndex;
    private string _apiHost = "localhost";
    private int _apiPort = 11434;
    private bool _changesTextVisibility;
    private bool _restartNeeded;

    private const string LanguageKey = "language";
    private const string ColorSchemeKey = "color-scheme";
    private const string ScrollToBottomKey = "scroll-to-bottom";
    private const string ApiHostKey = "api-host";
    private const string ApiPortKey = "api-port";

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
    
    public SettingsViewModel(DialogService dialogService, ConfigurationService configurationService)
    {
        DialogType = ApplicationDialog.Settings;
        _dialogService = dialogService;
        _configurationService = configurationService;
        ChangesTextVisibility = false;
        LoadSettings();
    }

    private void LoadSettings()
    {
        var language = _configurationService.ReadSetting(LanguageKey);
        SelectedLanguageIndex = language switch
        {
            "hungarian" => 0,
            _ => 1
        };
        
        _defaultLanguageIndex = SelectedLanguageIndex;
        RestartNeeded = false;
        
        var theme = _configurationService.ReadSetting(ColorSchemeKey);
        SelectedThemeIndex = theme switch
        {
            "light" => 0,
            _ => 1
        };

        var scrollToBottom = _configurationService.ReadSetting(ScrollToBottomKey);
        SelectedScrollIndex = scrollToBottom switch
        {
            "auto" => 0,
            "float" => 1,
            "none" => 2,
            _ => 1
        };
        
        var hostSetting = _configurationService.ReadSetting("api-host");
        ApiHost = string.IsNullOrEmpty(hostSetting) ? "localhost" : hostSetting;
        var portString = _configurationService.ReadSetting(ApiPortKey);
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
        _configurationService.SaveSetting(ColorSchemeKey, colorScheme);
        _configurationService.SaveSetting(LanguageKey, language);
        _configurationService.SaveSetting(ScrollToBottomKey, scrollToBottom);
        ChangesTextVisibility = true;

        if (!IsValidHost(ApiHost))
        {
            _dialogService.ShowErrorDialog(LocalizationService.GetString("INVALID_HOST_ERR"));
            ApiHost = _configurationService.ReadSetting(ApiHostKey);
            return;
        }
        
        if (!IsValidPort(ApiPort.ToString()))
        {
            _dialogService.ShowErrorDialog(LocalizationService.GetString("INVALID_PORT_ERR"));
            ApiPort = int.Parse(_configurationService.ReadSetting(ApiPortKey));
            return;
        }
        
        _configurationService.SaveSetting(ApiHostKey, ApiHost);
        _configurationService.SaveSetting(ApiPortKey, ApiPort.ToString());
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
    public void Close()
    {
        _dialogService.CloseDialog(ApplicationDialog.Settings);
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