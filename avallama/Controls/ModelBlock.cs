// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using avallama.Models;
using avallama.Utilities;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;

namespace avallama.Controls;

public class ModelBlock : Control
{
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
    
    public static readonly DirectProperty<ModelBlock, int> IndexProperty =
        AvaloniaProperty.RegisterDirect<ModelBlock, int>(
            nameof(Index),
            o => o.Index,
            (o, v) => o.Index = v,
            unsetValue: 0
        );

    public static readonly DirectProperty<ModelBlock, int> SelectedIndexProperty =
        AvaloniaProperty.RegisterDirect<ModelBlock, int>(
            nameof(SelectedIndex),
            o => o.SelectedIndex,
            (o, v) => o.SelectedIndex = v,
            unsetValue: 0
        );

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

    private int _index;

    public int Index
    {
        get => _index;
        set => SetAndRaise(IndexProperty, ref _index, value);
    }

    private int _selectedIndex;

    public int SelectedIndex
    {
        get => _selectedIndex;
        set => SetAndRaise(SelectedIndexProperty, ref _selectedIndex, value);
    }

    private static readonly Thickness BasePadding = new(12);
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
        var background = SelectedIndex == Index
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
        
        var textColor = SelectedIndex == Index
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
                InvalidateVisual();
                InvalidateMeasure();
                break;
            case nameof(DownloadStatus):
            case nameof(DownloadProgress):
            case nameof(SelectedIndex):
                InvalidateVisual();
                break;
        }
    }
}