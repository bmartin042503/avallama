// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System.Diagnostics;
using avallama.Services;
using CommunityToolkit.Mvvm.Input;

namespace avallama.ViewModels;

public partial class SettingsViewModel : DialogViewModel
{
    private readonly DialogService _dialogService;
    private const string Url = @"https://github.com/4foureyes/avallama/";

    public SettingsViewModel(DialogService dialogService)
    {
        _dialogService = dialogService;
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
}