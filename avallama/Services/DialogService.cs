// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using avallama.Constants;
using avallama.Factories;
using avallama.ViewModels;
using avallama.Views;
using avallama.Views.Dialogs;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace avallama.Services;

// TODO:
// Windowson legyen a dialognak drop shadowja, vagy valamivel legyen különböző a keret, mert egybeolvad a háttérrel
// async fix, lehessen több dialogot megjeleníteni egymás után

public enum ConfirmationType
{
    Positive,
    Negative
}

public abstract record DialogResult;
public record ConfirmationResult(ConfirmationType Confirmation) : DialogResult;
public record InputResult(string Input) : DialogResult;
public record NullResult(string ErrorMessage = ""): DialogResult;

public interface IDialogService
{
    void ShowDialog(ApplicationDialog dialog);
    void ShowInfoDialog(string informationMessage);
    void ShowErrorDialog(string errorMessage);
    Task<DialogResult> ShowConfirmationDialog(
        string title,
        string description,
        string positiveButtonText,
        string negativeButtonText,
        ConfirmationType highlight
    );
    Task<DialogResult> ShowInputDialog(
        string title,
        string description,
        string inputValue
    );
    void CloseDialog(ApplicationDialog dialog);
    void CloseDialog();
    void CloseAllDialogs();
}

public class DialogService(
    DialogViewModelFactory dialogViewModelFactory)
    : IDialogService
{
    private Stack<DialogWindow> _dialogStack = new();

    // létrehoz és megjelenít egy személyre szabott dialogot
    public void ShowDialog(ApplicationDialog dialog)
    {
        if (dialog is ApplicationDialog.Information 
            or ApplicationDialog.Error
            or ApplicationDialog.Confirmation
            or ApplicationDialog.Input)
        {
            throw new InvalidOperationException($"{dialog} dialog can not be used with ShowDialog.");
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

    /// <summary>
    /// Megjelenít egy megerősítésre váró dialogot a felhasználónak, két választható gombbal.
    /// A metódus aszinkron módon várja meg, míg a felhasználó valamelyik gombra kattint, 
    /// majd visszatér a választott eredménnyel egy <see cref="DialogResult"/> típusban.
    /// </summary>
    /// <param name="title">
    /// A dialog címe vagy kérdés szövege.
    /// </param>
    /// <param name="positiveButtonText">
    /// A bal oldali gomb szövege.
    /// </param>
    /// <param name="negativeButtonText">
    /// A jobb oldali gomb szövege.
    /// </param>
    /// <param name="description">
    /// A dialog leírása (elhagyható).
    /// </param>
    /// <param name="highlight">
    /// A kiemelendő gomb típusa, amely vizuálisan hangsúlyosabb lesz.
    /// </param>
    /// <returns>
    /// Egy <see cref="Task{DialogResult}"/>, amely tartalmazza a felhasználó választását.
    /// A visszatérési érték lehet <see cref="ConfirmationResult"/>, amely tartalmazza a választott <see cref="ConfirmationType"/> értéket.
    /// </returns>
    /// <example>
    /// Példa használatra:
    /// <code>
    /// var result = await ShowConfirmationDialog("Biztosan törölni szeretnéd ...?", "Törlés", "Mégsem", ConfirmationType.Negative);
    /// if (result is ConfirmationResult confRes)
    /// {
    ///     if (confRes.Confirmation == ConfirmationType.Positive)
    ///     {
    ///         // a felhasználó a pozitív (törlés) gombra nyomott
    ///     }
    /// }
    /// </code>
    /// </example>
    
    public async Task<DialogResult> ShowConfirmationDialog(
        string title,
        string positiveButtonText,
        string negativeButtonText,
        string description = "",
        ConfirmationType highlight = ConfirmationType.Positive
    )
    {
        var dialogResult = new TaskCompletionSource<DialogResult>();
        var dialogWindow = new DialogWindow();
        var type = typeof(DialogWindow).Assembly.GetType($"avallama.Views.Dialogs.ConfirmationView");
        if (type is null) return new NullResult("View type is null");
        var control = (Control)Activator.CreateInstance(type)! as ConfirmationView;
        
        control!.DialogTitle.Text = title;
        if (!string.IsNullOrEmpty(description))
        {
            control.DialogDescription.Text = description;
        }
        else
        {
            control.DialogDescription.IsVisible = false;
        }
        control.PositiveButton.Content = positiveButtonText;
        control.NegativeButton.Content = negativeButtonText;

        // ha a positivet akarjuk kiemelni, akkor a negativera adunk egy kevésbé hatásosabb megjelenést, és fordítva
        // ez a class a stylesban már definiálva van
        if (highlight == ConfirmationType.Positive)
        {
            control.NegativeButton.Classes.Add("lessSecondaryButton");
        }
        else if (highlight == ConfirmationType.Negative)
        {
            control.PositiveButton.Classes.Add("lessSecondaryButton");
        }

        control.PositiveButton.Click += (_, _) =>
        {
            dialogResult.TrySetResult(new ConfirmationResult(ConfirmationType.Positive));
            CloseDialog(ApplicationDialog.Confirmation);
        };
        
        control.NegativeButton.Click += (_, _) =>
        {
            dialogResult.TrySetResult(new ConfirmationResult(ConfirmationType.Negative));
            CloseDialog(ApplicationDialog.Confirmation);
        };
        
        dialogWindow.Content = control;
        dialogWindow.DataContext = new DialogViewModel
        {
            DialogType = ApplicationDialog.Confirmation
        };
        ShowDialogWindow(dialogWindow);
        var result = await dialogResult.Task;
        return result;
    }

    public async Task<DialogResult> ShowInputDialog(
        string title,
        string description = "",
        string inputValue = ""
    )
    {
        var dialogResult = new TaskCompletionSource<DialogResult>();
        var dialogWindow = new DialogWindow();
        var type = typeof(DialogWindow).Assembly.GetType($"avallama.Views.Dialogs.InputView");
        if (type is null) return new NullResult("View type is null");
        var control = (Control)Activator.CreateInstance(type)! as InputView;
        
        control!.DialogTitle.Text = title;
        control.InputTextBox.Text = inputValue;
        if (!string.IsNullOrEmpty(description))
        {
            control.DialogDescription.Text = description;
        }
        else
        {
            control.DialogDescription.IsVisible = false;
        }

        control.CloseButton.Click += (_, _) =>
        {
            // bezárás esetén nem ad vissza resultot, hisz a felhasználó nem akart menteni semmit
            dialogResult.TrySetResult(new NullResult());
            CloseDialog(ApplicationDialog.Input);
        };

        control.SaveButton.Click += (_, _) =>
        {
            dialogResult.TrySetResult(new InputResult(control.InputTextBox.Text ?? string.Empty));
            CloseDialog(ApplicationDialog.Input);
        };
        
        dialogWindow.Content = control;
        dialogWindow.DataContext = new DialogViewModel
        {
            DialogType = ApplicationDialog.Input
        };
        ShowDialogWindow(dialogWindow);
        var result = await dialogResult.Task;
        return result;
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