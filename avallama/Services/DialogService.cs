// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
using avallama.Constants;
using avallama.Factories;
using avallama.Views;
using avallama.Views.Dialogs;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace avallama.Services;

public interface IDialogService
{
    void ShowDialog(ApplicationDialog dialog);
    void ShowInfoDialog(string informationMessage);
    void ShowErrorDialog(string errorMessage);
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
        if (dialog is ApplicationDialog.Information or ApplicationDialog.Error)
        {
            throw new InvalidOperationException($"{dialog} dialog can not be used with ShowDialog. " +
                                                "Use ShowInfoDialog or ShowErrorDialog instead.");
        }
        
        if (_dialogs.ContainsKey(dialog))
            return;
        
        var dialogWindow = new DialogWindow();
        var dialogName = dialog + "View";
        var type = typeof(DialogWindow).Assembly.GetType($"avallama.Views.Dialogs.{dialogName}");
        if (type is null) return;
        var control = (Control)Activator.CreateInstance(type)!;
        control.DataContext = dialogViewModelFactory.GetDialogViewModel(dialog);
        dialogWindow.Content = control;
        ShowDialogWindow(dialogWindow);
        _dialogs.Add(dialog, dialogWindow);
        dialogWindow.Closing += (_, _) => _dialogs.Remove(dialog);
    }

    public void ShowInfoDialog(string informationMessage)
    {
        if (_dialogs.ContainsKey(ApplicationDialog.Information))
        {
            CloseDialog(ApplicationDialog.Information);
        }
        var dialogWindow = new DialogWindow();
        var type = typeof(DialogWindow).Assembly.GetType($"avallama.Views.Dialogs.InformationView");
        if (type is null) return;
        var control = (Control)Activator.CreateInstance(type)! as InformationView;
        control!.DialogMessage.Text = informationMessage;
        dialogWindow.Content = control;
        ShowDialogWindow(dialogWindow);
        _dialogs[ApplicationDialog.Information] = dialogWindow;
        dialogWindow.Closing += (_, _) => _dialogs.Remove(ApplicationDialog.Information);
    }
    
    public void ShowErrorDialog(string errorMessage)
    {
        if (_dialogs.ContainsKey(ApplicationDialog.Error))
        {
            CloseDialog(ApplicationDialog.Error);
        }
        var dialogWindow = new DialogWindow();
        var type = typeof(DialogWindow).Assembly.GetType($"avallama.Views.Dialogs.ErrorView");
        if (type is null) return;
        var control = (Control)Activator.CreateInstance(type)! as ErrorView;
        control!.DialogMessage.Text = errorMessage;
        dialogWindow.Content = control;
        ShowDialogWindow(dialogWindow);
        _dialogs[ApplicationDialog.Error] = dialogWindow;
        dialogWindow.Closing += (_, _) => _dialogs.Remove(ApplicationDialog.Error);
    }

    // segéd metódus, amely ténylegesen megjeleníti a dialogot úgy, hogy a MainWindowhoz csatolja (ha lehet)
    private void ShowDialogWindow(DialogWindow dialogWindow)
    {
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
    }

    public void CloseDialog(ApplicationDialog dialog)
    {
        if (!_dialogs.TryGetValue(dialog, out var dialogWindow)) return;
        dialogWindow.Close();
        _dialogs.Remove(dialog);
    }
}