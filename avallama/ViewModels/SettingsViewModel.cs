// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System.ComponentModel;
using System.Diagnostics;
using avallama.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace avallama.ViewModels;

public partial class SettingsViewModel : DialogViewModel
{
    private readonly DialogService _dialogService;
    private readonly ConfigurationService _configurationService;
    private const string Url = @"https://github.com/4foureyes/avallama/";

    private int _selectedLanguageIndex;
    private int _selectedThemeIndex;
    private int _defaultLanguageIndex;
    private bool _restartNeeded;

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
    
    public SettingsViewModel(DialogService dialogService, ConfigurationService configurationService)
    {
        _dialogService = dialogService;
        _configurationService = configurationService;
        LoadSettings();
    }

    private void LoadSettings()
    {
        var language = _configurationService.ReadSetting("language");
        SelectedLanguageIndex = language switch
        {
            "hungarian" => 0,
            _ => 1
        };
        
        _defaultLanguageIndex = SelectedLanguageIndex;
        RestartNeeded = false;
        
        var theme = _configurationService.ReadSetting("color-scheme");
        SelectedThemeIndex = theme switch
        {
            "light" => 0,
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
        _configurationService.SaveSetting("color-scheme", colorScheme);
        _configurationService.SaveSetting("language", language);
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
        _dialogService.CloseDialog();
    }

    [RelayCommand]
    public void Save()
    {
        SaveSettings();
    }
}