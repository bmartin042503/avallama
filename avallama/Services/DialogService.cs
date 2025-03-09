// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Threading.Tasks;
using avallama.Constants;
using avallama.Factories;
using avallama.Views;
using Avalonia.Controls;
using Avalonia.Controls.Templates;

namespace avallama.Services;

public interface IDialogService
{
    Task ShowDialog(ApplicationDialogContent dialogContent);
    void CloseDialog();
}

// Később hozzáadni hogy adott Dialog Resultot is visszaadhasson ha kell vagy viewmodel kezelné idk
public class DialogService : IDialogService
{
    private DialogWindow? _dialogWindow;
    private MainWindow _mainWindow;
    private DialogViewModelFactory _dialogViewModelFactory;

    public DialogService(DialogWindow dialogWindow, MainWindow mainWindow, DialogViewModelFactory dialogViewModelFactory)
    {
        _dialogWindow = dialogWindow;
        _mainWindow = mainWindow;
        _dialogViewModelFactory = dialogViewModelFactory;
        
        _dialogWindow.Closing += (s, e) =>
        {
            ((Window)s!).Hide();
            e.Cancel = true;
        }; 
    }
    
    public async Task ShowDialog(ApplicationDialogContent dialogContent)
    {
        if (_dialogWindow == null) return;
        var dialogViewModel = _dialogViewModelFactory.GetDialogViewModel(dialogContent);
        var dialogContentName = dialogContent + "View";
        var type = typeof(DialogWindow).Assembly.GetType($"avallama.Views.{dialogContentName}");;
        if (type is null) return;
        var control = (Control)Activator.CreateInstance(type)!;
        control.DataContext = dialogViewModel;
        _dialogWindow.Content = control;
        await _dialogWindow.ShowDialog(_mainWindow);
    }

    public void CloseDialog()
    {
        _dialogWindow?.Hide();    
    }
}