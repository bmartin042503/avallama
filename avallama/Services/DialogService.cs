// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using avallama.Constants;
using avallama.Factories;
using avallama.Views;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace avallama.Services;

public interface IDialogService
{
    void ShowDialog(ApplicationDialogContent dialogContent);
    void CloseDialog();
}

// Később hozzáadni hogy adott Dialog Resultot is visszaadhasson ha kell vagy viewmodel kezelné idk
// meg talán kibővíteni úgy hogy egyszerre több dialog is lehessen
public class DialogService(
    DialogWindow dialogWindow,
    DialogViewModelFactory dialogViewModelFactory)
    : IDialogService
{
    private DialogWindow? _dialogWindow = dialogWindow;

    public void ShowDialog(ApplicationDialogContent dialogContent)
    {
        _dialogWindow = new DialogWindow();
        var dialogViewModel = dialogViewModelFactory.GetDialogViewModel(dialogContent);
        var dialogContentName = dialogContent + "View";
        var type = typeof(DialogWindow).Assembly.GetType($"avallama.Views.{dialogContentName}");;
        if (type is null) return;
        var control = (Control)Activator.CreateInstance(type)!;
        control.DataContext = dialogViewModel;
        _dialogWindow.Content = control;
        var mainWindow =
            Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow
                : null;
        if (mainWindow == null)
        {
            _dialogWindow.Show();
        }
        else
        {
            _dialogWindow.ShowDialog(mainWindow);
        }
    }

    public void CloseDialog()
    {
        if (_dialogWindow != null)
        {
            _dialogWindow.Close();
            _dialogWindow = null;
        }  
    }
}