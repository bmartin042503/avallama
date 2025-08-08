// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Windows.Input;
using avallama.Models;
using avallama.Utilities;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;

namespace avallama.Controls;

public class ModelBlock : Control
{
    // azért Title a property neve, mert a 'Name' már le van foglalva és szerintem összekavarodna
    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<ModelBlock, string?>(nameof(Title));

    public static readonly DirectProperty<ModelBlock, ModelDownloadStatus> DownloadStatusProperty =
        AvaloniaProperty.RegisterDirect<ModelBlock, ModelDownloadStatus>(
            nameof(DownloadStatus),
            o => o.DownloadStatus,
            (o, v) => o.DownloadStatus = v
        );

    public static readonly DirectProperty<ModelBlock, double?> DownloadProgressProperty =
        AvaloniaProperty.RegisterDirect<ModelBlock, double?>(
            nameof(DownloadProgress),
            o => o.DownloadProgress,
            (o, v) => o.DownloadProgress = v
        );

    public static readonly DirectProperty<ModelBlock, string?> SelectedNameProperty =
        AvaloniaProperty.RegisterDirect<ModelBlock, string?>(
            nameof(SelectedName),
            o => o.SelectedName,
            (o, v) => o.SelectedName = v,
            unsetValue: string.Empty
        );
    
    public static readonly StyledProperty<ICommand> CommandProperty =
        AvaloniaProperty.Register<ModelInfoBlock, ICommand>("Command");

    public static readonly StyledProperty<object?> CommandParameterProperty =
        AvaloniaProperty.Register<ModelInfoBlock, object?>("CommandParameter");

    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    private ModelDownloadStatus _downloadStatus;
    public ModelDownloadStatus DownloadStatus
    {
        get => _downloadStatus;
        set => SetAndRaise(DownloadStatusProperty, ref _downloadStatus, value);
    }

    private double? _downloadProgress;
    public double? DownloadProgress
    {
        get => _downloadProgress;
        set => SetAndRaise(DownloadProgressProperty, ref _downloadProgress, value);
    }

    private string? _selectedName;

    public string? SelectedName
    {
        get => _selectedName;
        set => SetAndRaise(SelectedNameProperty, ref _selectedName, value);
    }
    
    public ICommand Command
    {
        get => GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    private static readonly Thickness BasePadding = new(10);
    private static readonly CornerRadius BaseCornerRadius = new(8);
    private const double TitleFontSize = 16;

    private TextLayout? _titleTextLayout;

    public override void Render(DrawingContext context)
    {
        // Háttér renderelése
        RenderBackground(context);
        
        // Szöveg kirenderelése
        RenderText(context);
    }

    private void RenderBackground(DrawingContext context)
    {
        var background = Title == SelectedName
            ? ColorProvider.GetColor(AppColor.Primary)
            : ColorProvider.GetColor(AppColor.SecondaryContainer);
        context.DrawRectangle(background, null,
            new RoundedRect(
                new Rect(Bounds.Size),
                BaseCornerRadius
            )
        );
    }

    private void RenderText(DrawingContext context)
    {
        _titleTextLayout?.Draw(
            context, 
            new Point(BasePadding.Left, BasePadding.Top)
        );
    }

    private TextLayout? CreateTextLayout()
    {
        if (string.IsNullOrEmpty(Title)) return null;
        
        var textColor = Title == SelectedName
            ? ColorProvider.GetColor(AppColor.OnPrimary)
            : ColorProvider.GetColor(AppColor.OnSecondaryContainer);

        return new TextLayout(
            Title,
            new Typeface(RenderHelper.ManropeFont),
            TitleFontSize,
            textColor,
            TextAlignment.Left,
            TextWrapping.Wrap,
            TextTrimming.CharacterEllipsis
        );
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        // kurzor átállítása, nem kell ellenőrizni hogy hol a kurzor, mert a PointerEventArgs alapból a Controlon belül van
        Cursor = new Cursor(StandardCursorType.Hand);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        if (Command.CanExecute(CommandParameter))
        {
            Command.Execute(CommandParameter);
        }
    }

    protected override void OnMeasureInvalidated()
    {
        _titleTextLayout?.Dispose();
        _titleTextLayout = null;
        base.OnMeasureInvalidated();
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        _titleTextLayout ??= CreateTextLayout();

        return new Size(
            width: availableSize.Width,
            height: BasePadding.Top + BasePadding.Bottom + (_titleTextLayout?.Height ?? 0.0)
        );
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        switch (change.Property.Name)
        {
            case nameof(Title):
            case nameof(DownloadStatus):
            case nameof(DownloadProgress):
            case nameof(SelectedName):
                InvalidateVisual();
                InvalidateMeasure();
                break;
        }
    }
}