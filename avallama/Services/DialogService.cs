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
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.Messaging;

namespace avallama.Services;

public enum ConfirmationType
{
    Positive,
    Negative
}

public class InputField(
    int maxLength = 255,
    string placeholder = "",
    string inputValue = "",
    bool isPassword = false,
    Func<string, bool>? validator = null,
    string validationErrorMessage = "")
{
    public int MaxLength { get; set; } = maxLength;
    public string Placeholder { get; set; } = placeholder;
    public string InputValue { get; set; } = inputValue;
    public bool IsPassword { get; set; } = isPassword;

    // a validatorban megadhatjuk, hogy mikor van egy érték helyesen megadva (pl. value => value.Contains("@")) ha emailre néznénk
    public Func<string, bool>? Validator { get; set; } = validator;

    // error üzenet ami akkor fog megjelenni ha a validálás nem járt sikerrel
    public string ValidationErrorMessage { get; set; } = validationErrorMessage;

    public bool IsValid => Validator == null || Validator(InputValue);
}

public abstract record DialogResult;

public record ConfirmationResult(ConfirmationType Confirmation) : DialogResult;

public record InputResult(IEnumerable<string?> Results) : DialogResult;

public record NullResult(string ErrorMessage = "") : DialogResult;

public interface IDialogService
{
    void ShowDialog(
        ApplicationDialog dialog,
        bool resizable,
        double width,
        double height,
        double minWidth,
        double minHeight,
        double maxWidth,
        double maxHeight
    );

    void ShowInfoDialog(string informationMessage);

    void ShowErrorDialog(
        string errorMessage,
        bool shutDownApp
    );

    void ShowActionDialog(
        string title,
        string actionButtonText,
        Action action,
        Action? closeAction,
        string description,
        bool actionButtonOnly
    );

    Task<DialogResult> ShowConfirmationDialog(
        string title,
        string description,
        string positiveButtonText,
        string negativeButtonText,
        ConfirmationType highlight
    );

    Task<DialogResult> ShowInputDialog(
        string title,
        IEnumerable<InputField> fields,
        string description
    );

    void CloseDialog(ApplicationDialog dialog);
    void CloseDialog();
    void CloseAllDialogs();
}

public class DialogService(
    DialogViewModelFactory dialogViewModelFactory,
    IMessenger messenger)
    : IDialogService
{
    private Stack<DialogWindow> _dialogStack = new();

    /// <summary>
    /// Megjelenít egy egyedi, személyre szabott dialogot.
    /// A megadott <see cref="ApplicationDialog"/> típushoz tartozó nézet alapján készíti el a dialogablakot.
    /// </summary>
    /// <param name="dialog">A megjelenítendő dialog típusa. Csak egyedi típus engedélyezett.</param>
    /// <param name="resizable">A dialog átméretezhetősége (opcionális)</param>
    /// <param name="width">A dialog ablakának szélessége (opcionális)</param>
    /// <param name="height">A dialog ablakának magassága (opcionális)</param>
    /// <param name="minWidth">A dialog ablakának minimális szélessége (ha átméretezhető)</param>
    /// <param name="minHeight">A dialog ablakának minimális magassága (ha átméretezhető)</param>
    /// <param name="maxWidth">A dialog ablakának maximális szélessége (ha átméretezhető)</param>
    /// <param name="maxHeight">A dialog ablakának maximális magassága (ha átméretezhető)</param>
    /// <exception cref="InvalidOperationException">
    /// Ha a dialog típusa nem megfelelő (pl. Information, Error, Confirmation, Input), kivételt dob.
    /// </exception>
    public void ShowDialog(
        ApplicationDialog dialog,
        bool resizable = false,
        double width = double.NaN,
        double height = double.NaN,
        double minWidth = double.NaN,
        double minHeight = double.NaN,
        double maxWidth = double.NaN,
        double maxHeight = double.NaN
    )
    {
        if (dialog is ApplicationDialog.Information
            or ApplicationDialog.Error
            or ApplicationDialog.Confirmation
            or ApplicationDialog.Input
            or ApplicationDialog.Action)
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
        dialogWindow.CanResize = resizable;

        // ha külön megadjuk a height vagy width értéket akkor az átméretezést eszerint állítja be
        // tehát ha csak width-et adunk meg akkor a magasságot a tartalomhoz igazítja és fordítva
        if (double.IsNaN(width) && !double.IsNaN(height))
        {
            dialogWindow.SizeToContent = SizeToContent.Width;
            dialogWindow.Height = height;
        }
        else if (!double.IsNaN(width) && double.IsNaN(height))
        {
            dialogWindow.SizeToContent = SizeToContent.Height;
            dialogWindow.Width = width;
        }
        else if (!double.IsNaN(height) && !double.IsNaN(width))
        {
            dialogWindow.SizeToContent = SizeToContent.Manual;
            dialogWindow.Height = height;
            dialogWindow.Width = width;
        }

        if (resizable)
        {
            if (!double.IsNaN(minWidth)) dialogWindow.MinWidth = minWidth;
            if (!double.IsNaN(minHeight)) dialogWindow.MinHeight = minHeight;
            if (!double.IsNaN(maxWidth)) dialogWindow.MaxWidth = maxWidth;
            if (!double.IsNaN(maxHeight)) dialogWindow.MaxHeight = maxHeight;
        }
        dialogWindow.InvalidateMeasure();
        ShowDialogWindow(dialogWindow);
    }

    /// <summary>
    /// Megjelenít egy információs dialogot, amely csak egy szöveges üzenetet tartalmaz.
    /// </summary>
    /// <param name="informationMessage">A megjelenítendő információs üzenet.</param>
    public void ShowInfoDialog(string informationMessage)
    {
        var dialogWindow = new DialogWindow();
        var type = typeof(DialogWindow).Assembly.GetType("avallama.Views.Dialogs.InformationView");
        if (type is null) return;
        var control = (Control)Activator.CreateInstance(type)! as InformationView;
        control!.DialogMessage.Text = informationMessage.Replace(@"\n", Environment.NewLine);
        dialogWindow.Content = control;
        dialogWindow.DataContext = new DialogViewModel
        {
            DialogType = ApplicationDialog.Information
        };
        ShowDialogWindow(dialogWindow);
    }

    /// <summary>
    /// Megjelenít egy hibaüzenetet tartalmazó dialogot, amely figyelmezteti a felhasználót valamilyen problémára.
    /// </summary>
    /// <param name="errorMessage">A megjelenítendő hibaüzenet.</param>
    /// <param name="shutdownApp">Az alkalmazás leállítása a dialog bezárását követően (opcionális).</param>
    public void ShowErrorDialog(
        string errorMessage,
        bool shutdownApp = false
    )
    {
        var dialogWindow = new DialogWindow();
        var type = typeof(DialogWindow).Assembly.GetType("avallama.Views.Dialogs.ErrorView");
        if (type is null) return;
        var control = (Control)Activator.CreateInstance(type)! as ErrorView;
        control!.DialogMessage.Text = errorMessage.Replace(@"\n", Environment.NewLine);
        control.CloseButton.Click += (_, _) =>
        {
            CloseDialog(ApplicationDialog.Error);
            if (shutdownApp)
            {
                messenger.Send(new ApplicationMessage.Shutdown());
            }
        };

        dialogWindow.Content = control;
        dialogWindow.DataContext = new DialogViewModel
        {
            DialogType = ApplicationDialog.Error
        };
        ShowDialogWindow(dialogWindow);
    }

    /// <summary>
    /// Megjelenít egy cselekvésre felszólító dialogot, egy megadható <see cref="Action"/> metódussal, ami lefut, ha a
    /// dialogban lévő action (bal oldali) gombra kattintanak.
    /// </summary>
    /// <param name="title">
    /// A dialog címe vagy kérdés szövege.
    /// </param>
    /// <param name="actionButtonText">
    /// A bal oldali gomb szövege.
    /// </param>
    /// <param name="action">
    /// A metódus ami lefut az action gomb megnyomása esetén.
    /// </param>
    /// <param name="closeAction">
    /// A metódus ami lefut a bezárás gomb megnyomása esetén (opcionális).
    /// </param>
    /// <param name="description">
    /// A dialog leírása (opcionális).
    /// </param>
    /// <param name="actionButtonOnly">
    /// Csak az action button jelenjen meg-e, tehát a bezárás gomb nélkül (opcionális).
    /// </param>
    /// <example>
    /// Példa használatra:
    /// <code>
    /// _dialogService.ShowActionDialog(
    ///     title: LocalizationService.GetString("OLLAMA_NOT_INSTALLED"),
    ///     actionButtonText:  LocalizationService.GetString("DOWNLOAD"),
    ///     action: RedirectToOllamaDownload,
    ///     closeAction: _appService.Shutdown,
    ///     description: LocalizationService.GetString("OLLAMA_NOT_INSTALLED_DESC")
    /// );
    /// </code>
    /// </example>
    public void ShowActionDialog(
        string title,
        string actionButtonText,
        Action action,
        Action? closeAction = null,
        string description = "",
        bool actionButtonOnly = false
    )
    {
        var dialogWindow = new DialogWindow();
        // szándékosan ConfirmationView, hisz annak a View-ja újrafelhasználható
        var type = typeof(DialogWindow).Assembly.GetType("avallama.Views.Dialogs.ConfirmationView");
        if (type is null) return;
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

        control.PositiveButton.Content = actionButtonText;
        if (actionButtonOnly)
        {
            control.NegativeButton.IsVisible = false;
        }
        else
        {
            control.NegativeButton.Content = LocalizationService.GetString("CLOSE");
            control.NegativeButton.Classes.Add("secondaryButton");
            control.NegativeButton.Click += (_, _) =>
            {
                CloseDialog(ApplicationDialog.Action);
                closeAction?.Invoke();
            };
        }

        control.PositiveButton.Click += (_, _) =>
        {
            CloseDialog(ApplicationDialog.Action);
            action();
        };

        dialogWindow.Content = control;
        dialogWindow.DataContext = new DialogViewModel
        {
            DialogType = ApplicationDialog.Action
        };

        ShowDialogWindow(dialogWindow);
    }

    // azért kell hogy async legyenek ezek a metódusok, mert resulttal rendelkeznek és ezeket megfelelően be kell várni
    // ha nem async lenne akkor a UI szálon próbálná bevárni a resultot, ezáltal az lefagyna és nem lehetne beállítani resultot TrySetResult-al hiszen a UI szál már foglalt

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
    /// A dialog leírása (opcionális).
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
    /// var result = await _dialogService.ShowConfirmationDialog("Biztosan törölni szeretnéd ...?", "Törlés", "Mégsem", ConfirmationType.Negative);
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
        var type = typeof(DialogWindow).Assembly.GetType("avallama.Views.Dialogs.ConfirmationView");
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
            control.NegativeButton.Classes.Add("secondaryButton");
        }
        else if (highlight == ConfirmationType.Negative)
        {
            control.PositiveButton.Classes.Add("secondaryButton");
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

        // pl. ha a felhasználó valahogy más módon bezárja a dialogot akkor lekezeljük a taskot
        dialogWindow.Closing += (_, _) =>
        {
            if (!dialogResult.Task.IsCompleted)
            {
                dialogResult.TrySetResult(new NullResult("DialogWindow closed before returning result"));
            }
        };
        await ShowDialogWindowAsync(dialogWindow);
        var result = await dialogResult.Task;
        return result;
    }

    /// <summary>
    /// Megjelenít TextBox mező(ke)t tartalmazó dialogot a felhasználónak, amely lehetővé teszi több szöveg megadását beviteli mezőkön keresztül, mint <see cref="InputField"/> típus.
    /// A dialog két gombot tartalmaz: mentés és bezárás. A felhasználó válasza alapján visszatér egy <see cref="DialogResult"/>-tel.
    /// </summary>
    /// <param name="title">A dialog címe.</param>
    /// <param name="description">Kiegészítő leírás a dialoghoz (opcionális).</param>
    /// <param name="inputFields">Beviteli mezők</param>
    /// <returns>
    /// Egy <see cref="Task{DialogResult}"/> amely tartalmazza a felhasználó által megadott szöveget <see cref="InputResult"/> formájában,
    /// vagy <see cref="NullResult"/>-ot, ha a dialog bezárásra került válasz nélkül.
    /// </returns>
    /// <example>
    /// Példa használatra:
    /// <code>
    /// var dialogResult = await _dialogService.ShowInputDialog(
    ///     title: LocalizationService.GetString("OLLAMA_REMOTE_DIALOG_TITLE"),
    ///     description: LocalizationService.GetString("OLLAMA_REMOTE_DIALOG_DESC"), 
    ///     inputFields: new List()
    ///     {
    ///         new (placeholder: LocalizationService.GetString("API_HOST_SETTING")),
    ///         new (
    ///             placeholder: LocalizationService.GetString("API_PORT_SETTING"),
    ///             inputValue: 11434.ToString()
    ///         )
    ///     }
    /// );
    ///     
    /// if (dialogResult is InputResult inputResult)
    /// {
    ///     var count = 0;
    ///     foreach (var result in inputResult.Results)
    ///     {
    ///         Console.WriteLine($"({count}) Input Field Value: {result}");
    ///         count++;
    ///     }
    /// }
    /// </code>
    /// </example>
    public async Task<DialogResult> ShowInputDialog(
        string title,
        IEnumerable<InputField> inputFields,
        string description = ""
    )
    {
        var fields = inputFields.ToList();
        if (fields.Count == 0) return new NullResult("Empty input fields");

        var dialogResult = new TaskCompletionSource<DialogResult>();
        var dialogWindow = new DialogWindow();
        var type = typeof(DialogWindow).Assembly.GetType("avallama.Views.Dialogs.InputView");
        if (type is null) return new NullResult("View type is null");
        var control = (Control)Activator.CreateInstance(type)! as InputView;

        control!.DialogTitle.Text = title;
        if (!string.IsNullOrEmpty(description))
        {
            control.DialogDescription.Text = description;
        }
        else
        {
            control.DialogDescription.IsVisible = false;
        }

        control.InputFieldsStackPanel.Children.Clear();
        control.ErrorMessage.IsVisible = false;

        foreach (var field in fields)
        {
            var inputTextBox = new TextBox();
            inputTextBox.Classes.Add("settingTextBox");

            if (field.IsPassword)
            {
                inputTextBox.PasswordChar = '*';
            }

            if (!string.IsNullOrEmpty(field.Placeholder))
                inputTextBox.Watermark = field.Placeholder;

            if (!string.IsNullOrEmpty(field.InputValue))
                inputTextBox.Text = field.InputValue;

            inputTextBox.MaxLength = field.MaxLength;
            control.InputFieldsStackPanel.Children.Add(inputTextBox);
        }

        control.CloseButton.Click += (_, _) =>
        {
            // bezárás esetén nem ad vissza resultot, hisz a felhasználó nem akart menteni semmit
            dialogResult.TrySetResult(new NullResult());
            CloseDialog(ApplicationDialog.Input);
        };

        control.SaveButton.Click += (_, _) =>
        {
            control.ErrorMessage.IsVisible = false;
            control.ErrorMessage.Text = string.Empty;
            
            // validation check
            for (var i = 0; i < fields.Count; i++)
            {
                if (control.InputFieldsStackPanel.Children[i] is TextBox fieldTextBox)
                {
                    // frissítjük az inputfieldek tartalmát hogy lehessen újravalidálni
                    fields[i].InputValue = fieldTextBox.Text ?? string.Empty;
                }

                if (!fields[i].IsValid)
                {
                    control.ErrorMessage.IsVisible = true;
                    control.ErrorMessage.Text = fields[i].ValidationErrorMessage;
                    return;
                }
            }
            
            // a textboxok, vagyis a beviteli mezők szövegeit összevonja egy List<string>-be
            var inputList = control.InputFieldsStackPanel.Children.Select(item =>
                    item as TextBox
                )
                .OfType<TextBox>()
                .Select(textBoxItem => textBoxItem.Text)
                .ToList();

            dialogResult.TrySetResult(new InputResult(inputList));
            CloseDialog(ApplicationDialog.Input);
        };

        dialogWindow.Content = control;
        dialogWindow.DataContext = new DialogViewModel
        {
            DialogType = ApplicationDialog.Input
        };

        // pl. ha a felhasználó valahogy más módon bezárja a dialogot akkor lekezeljük a taskot
        dialogWindow.Closing += (_, _) =>
        {
            if (!dialogResult.Task.IsCompleted)
            {
                dialogResult.TrySetResult(new NullResult("DialogWindow closed before returning result"));
            }
        };
        await ShowDialogWindowAsync(dialogWindow);
        var result = await dialogResult.Task;
        return result;
    }

    /// <summary>
    /// Megjelenít egy dialogot, amely a dialog stack tetejére kerül.
    /// Ha nincs nyitott dialog, akkor a MainWindow-hoz csatolja.
    /// </summary>
    /// <param name="dialogWindow">A megjelenítendő dialog ablak.</param>
    private void ShowDialogWindow(DialogWindow dialogWindow)
    {
        var parent = _dialogStack.Count > 0
            ? _dialogStack.Peek()
            : Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
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

    /// <summary>
    /// Aszinkron módon jelenít meg egy dialogot, amely a dialog stack tetejére kerül.
    /// Ha nincs nyitott dialog, akkor a MainWindow-hoz csatolja. A metódus megvárja, míg a dialog bezárul.
    /// </summary>
    /// <param name="dialogWindow">A megjelenítendő dialog ablak.</param>
    /// <returns>
    /// Egy <see cref="Task"/> amely akkor fejeződik be, amikor a dialog bezárásra kerül.
    /// </returns>
    private async Task ShowDialogWindowAsync(DialogWindow dialogWindow)
    {
        var parent = _dialogStack.Count > 0
            ? _dialogStack.Peek()
            : Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow
                : null;
        
        _dialogStack.Push(dialogWindow);
        dialogWindow.Closing += (_, _) =>
        {
            if (_dialogStack.Peek() == dialogWindow)
                _dialogStack.Pop();
            else
                _dialogStack = new Stack<DialogWindow>(_dialogStack.Where(w => w != dialogWindow));
        };
        // fontos hogy az await-os dialog megjelenítés legutoljára legyen, mert az await-al egészen várni fog addig amíg be nem zárul a dialog
        // és ha nem állítjuk be a dialogot előtte (hozzáadni stackhez, closinghoz műveletek hozzáadása) akkor nem is lehet bezárni
        if (parent == null)
        {
            dialogWindow.Show();
        }
        else
        {
            await dialogWindow.ShowDialog(parent);
        }
    }

    /// <summary>
    /// Bezárja a megadott típusú dialogot, ha megtalálható a stackben.
    /// </summary>
    /// <param name="dialog">A bezárandó dialog típusa.</param>
    public void CloseDialog(ApplicationDialog dialog)
    {
        var dialogWindow = _dialogStack.FirstOrDefault(d => d.DataContext is DialogViewModel viewModel
                                                            && viewModel.DialogType == dialog);
        if (dialogWindow == null) return;
        dialogWindow.Close();
        _dialogStack = new Stack<DialogWindow>(_dialogStack.Where(w => w != dialogWindow));
    }

    /// <summary>
    /// Bezárja a legutóbb megnyitott dialogot a stack tetejéről.
    /// </summary>
    public void CloseDialog()
    {
        if (_dialogStack.Count <= 0) return;
        var dialogWindow = _dialogStack.Pop();
        dialogWindow.Close();
    }

    /// <summary>
    /// Bezárja az összes nyitott dialogot és kiüríti a dialog stacket.
    /// </summary>
    public void CloseAllDialogs()
    {
        foreach (var dialogWindow in _dialogStack.ToList())
        {
            dialogWindow.Close();
        }

        _dialogStack.Clear();
    }
}