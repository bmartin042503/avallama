// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using avallama.Constants;
using avallama.Factories;
using avallama.Views;
using Avalonia.Controls;

namespace avallama.Services;

public interface IDialogService
{
    void ShowDialog(ApplicationDialogContent dialogContent);
    void CloseDialog();
}

// Később hozzáadni hogy adott Dialog Resultot is visszaadhasson ha kell vagy viewmodel kezelné idk
// meg talán kibővíteni úgy hogy egyszerre több dialog is lehessen
public class DialogService : IDialogService
{
    private readonly DialogWindow? _dialogWindow;
    private readonly DialogViewModelFactory _dialogViewModelFactory;

    public DialogService(
        DialogWindow dialogWindow, 
        DialogViewModelFactory dialogViewModelFactory
    )
    {
        _dialogWindow = dialogWindow;
        _dialogViewModelFactory = dialogViewModelFactory;
        
        _dialogWindow.Closing += (s, e) =>
        {
            ((Window)s!).Hide();
            e.Cancel = true;
        }; 
    }
    
    public void ShowDialog(ApplicationDialogContent dialogContent)
    {
        if (_dialogWindow == null) return;
        var dialogViewModel = _dialogViewModelFactory.GetDialogViewModel(dialogContent);
        var dialogContentName = dialogContent + "View";
        var type = typeof(DialogWindow).Assembly.GetType($"avallama.Views.{dialogContentName}");;
        if (type is null) return;
        var control = (Control)Activator.CreateInstance(type)!;
        control.DataContext = dialogViewModel;
        _dialogWindow.Content = control;
        _dialogWindow.Show();
    }

    public void CloseDialog()
    {
        _dialogWindow?.Hide();    
    }
}