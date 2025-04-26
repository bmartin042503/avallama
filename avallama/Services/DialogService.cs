// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
using System.Linq;
using avallama.Constants;
using avallama.Factories;
using avallama.Views;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace avallama.Services;

public interface IDialogService
{
    void ShowDialog(ApplicationDialog dialog);
    void CloseDialog(ApplicationDialog dialog);
}

// Később hozzáadni hogy adott Dialog Resultot is visszaadhasson ha kell vagy viewmodel kezelné idk
// meg talán kibővíteni úgy hogy egyszerre több dialog is lehessen
public class DialogService(
    DialogViewModelFactory dialogViewModelFactory)
    : IDialogService
{
    private readonly Dictionary<ApplicationDialog, DialogWindow> _dialogs = [];

    // létrehozza és megjeleníti a dialogot egy új dialogwindowban
    public void ShowDialog(ApplicationDialog dialog)
    {
        if (_dialogs.ContainsKey(dialog))
            return;
        
        var dialogWindow = new DialogWindow();
        var dialogViewModel = dialogViewModelFactory.GetDialogViewModel(dialog);
        var dialogName = dialog + "View";
        var type = typeof(DialogWindow).Assembly.GetType($"avallama.Views.Dialogs.{dialogName}");;
        if (type is null) return;
        var control = (Control)Activator.CreateInstance(type)!;
        control.DataContext = dialogViewModel;
        dialogWindow.Content = control;
        var mainWindow =
            Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow
                : null;
        if (mainWindow == null)
        {
            dialogWindow.Show();
        }
        else
        {
            dialogWindow.ShowDialog(mainWindow);
        }
        _dialogs.Add(dialog, dialogWindow);
        dialogWindow.Closing += (_, _) => _dialogs.Remove(dialog);
    }

    public void CloseDialog(ApplicationDialog dialog)
    {
        if (!_dialogs.TryGetValue(dialog, out var dialogWindow)) return;
        dialogWindow.Close();
        _dialogs.Remove(dialog);
    }
}