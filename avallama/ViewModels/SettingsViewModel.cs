// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using avallama.Services;
using CommunityToolkit.Mvvm.Input;

namespace avallama.ViewModels;

public partial class SettingsViewModel : DialogViewModel
{
    private readonly DialogService _dialogService;

    public SettingsViewModel(DialogService dialogService)
    {
        _dialogService = dialogService;
    }

    [RelayCommand]
    public void Close()
    {
        _dialogService.CloseDialog();
    }
}