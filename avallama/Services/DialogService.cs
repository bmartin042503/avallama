// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
using System.Linq;
using avallama.Constants;
using avallama.Factories;
using avallama.ViewModels;
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
    void CloseDialog();
    void CloseAllDialogs();
}

// Később hozzáadni hogy adott Dialog Resultot is visszaadhasson ha kell vagy viewmodel kezelné idk
public class DialogService(
    DialogViewModelFactory dialogViewModelFactory)
    : IDialogService
{
    private Stack<DialogWindow> _dialogStack = new();

    // létrehozza és megjeleníti a dialogot egy új dialogwindowban
    public void ShowDialog(ApplicationDialog dialog)
    {
        if (dialog is ApplicationDialog.Information or ApplicationDialog.Error)
        {
            throw new InvalidOperationException($"{dialog} dialog can not be used with ShowDialog. " +
                                                "Use ShowInfoDialog or ShowErrorDialog instead.");
        }
        
        var dialogWindow = new DialogWindow();
        var dialogName = dialog + "View";
        var type = typeof(DialogWindow).Assembly.GetType($"avallama.Views.Dialogs.{dialogName}");
        if (type is null) return;
        var control = (Control)Activator.CreateInstance(type)!;
        control.DataContext = dialogViewModelFactory.GetDialogViewModel(dialog);
        dialogWindow.DataContext = new DialogViewModel { DialogType = dialog };
        dialogWindow.Content = control;
        ShowDialogWindow(dialogWindow);
    }

    public void ShowInfoDialog(string informationMessage)
    {
        var dialogWindow = new DialogWindow();
        var type = typeof(DialogWindow).Assembly.GetType($"avallama.Views.Dialogs.InformationView");
        if (type is null) return;
        var control = (Control)Activator.CreateInstance(type)! as InformationView;
        control!.DialogMessage.Text = informationMessage;
        dialogWindow.Content = control;
        dialogWindow.DataContext = new DialogViewModel
        {
            DialogType = ApplicationDialog.Information
        };
        ShowDialogWindow(dialogWindow);
    }
    
    public void ShowErrorDialog(string errorMessage)
    {
        var dialogWindow = new DialogWindow();
        var type = typeof(DialogWindow).Assembly.GetType($"avallama.Views.Dialogs.ErrorView");
        if (type is null) return;
        var control = (Control)Activator.CreateInstance(type)! as ErrorView;
        control!.DialogMessage.Text = errorMessage;
        dialogWindow.Content = control;
        dialogWindow.DataContext = new DialogViewModel
        {
            DialogType = ApplicationDialog.Error
        };
        ShowDialogWindow(dialogWindow);
    }

    // segéd metódus, amely ténylegesen megjeleníti a dialogot úgy, hogy az előző dialoghoz vagy ha nincs ilyen akkor a MainWindowhoz csatolja
    private void ShowDialogWindow(DialogWindow dialogWindow)
    {
        var parent = _dialogStack.Count > 0
            ? _dialogStack.Peek()
            : Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow
                : null;
        if (parent == null)
        {
            dialogWindow.Show();
        }
        else
        {
            dialogWindow.ShowDialog(parent);
        }
        _dialogStack.Push(dialogWindow);
        dialogWindow.Closing += (_, _) =>
        {
            if (_dialogStack.Peek() == dialogWindow)
                _dialogStack.Pop();
            else
                _dialogStack = new Stack<DialogWindow>(_dialogStack.Where(w => w != dialogWindow));
        };
    }

    public void CloseDialog(ApplicationDialog dialog)
    {
        var dialogWindow = _dialogStack.FirstOrDefault(d => d.DataContext is DialogViewModel viewModel
            && viewModel.DialogType == dialog);
        if (dialogWindow == null) return;
        dialogWindow.Close();
        _dialogStack = new Stack<DialogWindow>(_dialogStack.Where(w => w != dialogWindow));
    }

    public void CloseDialog()
    {
        if (_dialogStack.Count <= 0) return;
        var dialogWindow = _dialogStack.Pop();
        dialogWindow.Close();
    }

    public void CloseAllDialogs()
    {
        foreach (var dialogWindow in _dialogStack.ToList())
        {
            dialogWindow.Close();
        }
        _dialogStack.Clear();
    }
}