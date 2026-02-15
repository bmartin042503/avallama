// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System.Windows.Input;
using avallama.Models.Download;
using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Input;

namespace avallama.Styles.TemplatedControls;

public class ModelListItem : TemplatedControl
{
    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<ModelListItem, string?>(nameof(Title));

    // This is a styled property so AXAML will work better (e.g. for preview)
    public static readonly StyledProperty<ModelDownloadStatus?> DownloadStatusProperty =
        AvaloniaProperty.Register<ModelListItem, ModelDownloadStatus?>(nameof(DownloadStatus));

    public static readonly DirectProperty<ModelListItem, double?> DownloadProgressProperty =
        AvaloniaProperty.RegisterDirect<ModelListItem, double?>(
            nameof(DownloadProgress),
            o => o.DownloadProgress,
            (o, v) => o.DownloadProgress = v
        );

    public static readonly DirectProperty<ModelListItem, string?> SelectedNameProperty =
        AvaloniaProperty.RegisterDirect<ModelListItem, string?>(
            nameof(SelectedName),
            o => o.SelectedName,
            (o, v) => o.SelectedName = v,
            unsetValue: string.Empty
        );

    public static readonly StyledProperty<string?> StatusTextProperty =
        AvaloniaProperty.Register<ModelItem, string?>(nameof(StatusText));

    public static readonly StyledProperty<ICommand> CommandProperty =
        AvaloniaProperty.Register<ModelListItem, ICommand>(nameof(Command));

    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public ModelDownloadStatus? DownloadStatus
    {
        get => GetValue(DownloadStatusProperty);
        set => SetValue(DownloadStatusProperty, value);
    }

    public double? DownloadProgress
    {
        get;
        set => SetAndRaise(DownloadProgressProperty, ref field, value);
    }

    public string? SelectedName
    {
        get;
        set => SetAndRaise(SelectedNameProperty, ref field, value);
    }

    public string? StatusText
    {
        get => GetValue(StatusTextProperty);
        set => SetValue(StatusTextProperty, value);
    }

    public ICommand Command
    {
        get => GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        if (Title is null) return;

        if (Command is { } cmd && cmd.CanExecute(Title))
        {
            cmd.Execute(Title);
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        switch (change.Property.Name)
        {
            case nameof(SelectedName):
                if (!string.IsNullOrEmpty(SelectedName) && !string.IsNullOrEmpty(Title))
                {
                    if (SelectedName == Title)
                    {
                        Classes.Add("selectedListItem");
                    }
                    else
                    {
                        Classes.Remove("selectedListItem");
                    }
                }
                break;
        }
    }
}
