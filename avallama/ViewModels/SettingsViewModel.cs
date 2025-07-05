// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System.Diagnostics;
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
    private bool _changesTextVisibility;
    private bool _restartNeeded;

    private const string LanguageKey = "language";
    private const string ColorSchemeKey = "color-scheme";
    private const string ScrollToBottomKey = "scroll-to-bottom";

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
}