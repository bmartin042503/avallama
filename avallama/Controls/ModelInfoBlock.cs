// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Input;
using avallama.Models;
using avallama.Services;
using avallama.Utilities;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Media.TextFormatting;
using Avalonia.Platform;
using Avalonia.Svg;
using ShimSkiaSharp;
using Svg.Model;

namespace avallama.Controls;

public class ModelInfoBlock : Control
{
    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<ModelInfoBlock, string?>("Title");

    public static readonly StyledProperty<int?> QuantizationProperty =
        AvaloniaProperty.Register<ModelInfoBlock, int?>("Quantization");

    public static readonly StyledProperty<double?> ParametersProperty =
        AvaloniaProperty.Register<ModelInfoBlock, double?>("Parameters");

    public static readonly StyledProperty<string?> FormatProperty =
        AvaloniaProperty.Register<ModelInfoBlock, string?>("Format");

    public static readonly StyledProperty<IDictionary<string, string>?> DetailItemsSourceProperty =
        AvaloniaProperty.Register<ModelInfoBlock, IDictionary<string, string>?>("DetailItemsSource");

    public static readonly StyledProperty<long> SizeInBytesProperty =
        AvaloniaProperty.Register<ModelInfoBlock, long>("SizeInBytes");

    public static readonly StyledProperty<ModelDownloadStatus> DownloadStatusProperty =
        AvaloniaProperty.Register<ModelInfoBlock, ModelDownloadStatus>("DownloadStatus");

    public static readonly StyledProperty<double?> DownloadProgressProperty =
        AvaloniaProperty.Register<ModelInfoBlock, double?>("DownloadProgress");

    public static readonly StyledProperty<bool> RunsSlowProperty =
        AvaloniaProperty.Register<ModelInfoBlock, bool>("RunsSlow");

    public static readonly StyledProperty<ICommand> CommandProperty =
        AvaloniaProperty.Register<ModelInfoBlock, ICommand>("Command");

    public static readonly StyledProperty<object?> CommandParameterProperty =
        AvaloniaProperty.Register<ModelInfoBlock, object?>("CommandParameter");


    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public int? Quantization
    {
        get => GetValue(QuantizationProperty);
        set => SetValue(QuantizationProperty, value);
    }

    public double? Parameters
    {
        get => GetValue(ParametersProperty);
        set => SetValue(ParametersProperty, value);
    }

    public string? Format
    {
        get => GetValue(FormatProperty);
        set => SetValue(FormatProperty, value);
    }

    public IDictionary<string, string>? DetailItemsSource
    {
        get => GetValue(DetailItemsSourceProperty);
        set => SetValue(DetailItemsSourceProperty, value);
    }

    public long SizeInBytes
    {
        get => GetValue(SizeInBytesProperty);
        set => SetValue(SizeInBytesProperty, value);
    }

    public ModelDownloadStatus DownloadStatus
    {
        get => GetValue(DownloadStatusProperty);
        set => SetValue(DownloadStatusProperty, value);
    }

    public double? DownloadProgress
    {
        get => GetValue(DownloadProgressProperty);
        set => SetValue(DownloadProgressProperty, value);
    }

    public bool RunsSlow
    {
        get => GetValue(RunsSlowProperty);
        set => SetValue(RunsSlowProperty, value);
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
    
    private const double TitleFontSize = 22;
    private const double DetailsFontSize = 12;
    private const double SizeFontSize = 14;
    private const double LabelFontSize = 9;
    private const double DownloadInfoFontSize = 12;
    private const double DetailsOpacity = 0.8;
    private const double DownloadInfoOpacity = 0.5;
    private const double SizeOpacity = 0.8;
    private const double DetailsLineHeight = 16;
    
    private static readonly Thickness BasePadding = new(12);

    private const string DownloadSvgPath = "Assets/Svg/download.svg";
    private const string PauseSvgPath = "Assets/Svg/pause.svg";

    private TextLayout? _titleTextLayout;
    private TextLayout? _detailsTextLayout;
    private IEnumerable<TextLayout>? _labelTextLayouts;
    private TextLayout? _sizeTextLayout;
    private TextLayout? _downloadInfoTextLayout;

    private SKPicture? _svgPicture;
    private AvaloniaPicture? _avaloniaSvgPicture;

    private double _renderXPos;
    private double _renderYPos;

    public override void Render(DrawingContext context)
    {
        
    }

    // TextLayoutok létrehozása
    private TextLayout? CreateTitleTextLayout()
    {
        if (string.IsNullOrEmpty(Title)) return null;

        return new TextLayout(
            Title,
            new Typeface(RenderHelper.ManropeFont),
            null,
            TitleFontSize,
            ColorProvider.GetColor(AppColor.OnSurface)
        );
    }

    private TextLayout? CreateDetailsTextLayout()
    {
        if (DetailItemsSource == null || DetailItemsSource.Count == 0) return null;

        var mergedDetailsText = string.Join('\n', DetailItemsSource.Select(kv => $"{kv.Key}: {kv.Value}"));

        return new TextLayout(
            mergedDetailsText,
            new Typeface(RenderHelper.ManropeFont, weight: FontWeight.Light),
            null,
            DetailsFontSize,
            new SolidColorBrush(
                ColorProvider.GetColor(AppColor.OnSurface).Color,
                DetailsOpacity
            ),
            lineHeight: DetailsLineHeight
        );
    }

    private TextLayout? CreateDownloadInfoTextLayout()
    {
        if (DownloadStatus is ModelDownloadStatus.Downloaded or ModelDownloadStatus.ReadyForDownload) return null;

        var downloadText = DownloadStatus switch
        {
            ModelDownloadStatus.NoConnectionForDownload => LocalizationService.GetString("NO_CONNECTION"),
            ModelDownloadStatus.NotEnoughSpaceForDownload => LocalizationService.GetString("NOT_ENOUGH_SPACE"),
            ModelDownloadStatus.Downloading => string.Format(LocalizationService.GetString("DOWNLOADING"),
                DownloadProgress?.ToString("0.00", CultureInfo.InvariantCulture)),
            _ => string.Empty
        };

        return new TextLayout(
            downloadText,
            new Typeface(RenderHelper.ManropeFont),
            null,
            DownloadInfoFontSize,
            new SolidColorBrush(
                ColorProvider.GetColor(AppColor.OnSurface).Color,
                DownloadInfoOpacity
            )
        );
    }

    private string GetSizeInGb()
    {
        var sizeInGb = SizeInBytes / (1024.0 * 1024.0 * 1024.0);
        var rounded = Math.Round(sizeInGb, 1);

        // ha a tizedesjegy nulla, ne jelenjen meg
        var displayValue = rounded % 1 == 0
            ? ((int)rounded).ToString()
            : rounded.ToString("0.0", CultureInfo.InvariantCulture);

        return string.Format(LocalizationService.GetString("SIZE_IN_GB"), displayValue);
    }

    protected override void OnMeasureInvalidated()
    {
        // felszabadítja a textLayoutokat
        _titleTextLayout?.Dispose();
        _titleTextLayout = null;
        _detailsTextLayout?.Dispose();
        _detailsTextLayout = null;
        _sizeTextLayout?.Dispose();
        _sizeTextLayout = null;
        if (_labelTextLayouts != null)
        {
            foreach (var labelTextLayout in _labelTextLayouts)
            {
                labelTextLayout.Dispose();
            }

            _labelTextLayouts = null;
        }

        _downloadInfoTextLayout?.Dispose();
        _downloadInfoTextLayout = null;

        base.OnMeasureInvalidated();
    }

    // megméri hogy mekkora a Control és ezt a méretet visszaadja
    // ez kell pl. ahhoz hogy megfelelő magasságot tudjon beállítani neki, ha rendereli a hátteret (Bounds.Height) stb.
    
    /*
    protected override Size MeasureOverride(Size availableSize)
    {
        _titleTextLayout ??= CreateTitleTextLayout();
        _detailsTextLayout ??= CreateDetailsTextLayout();
        _labelTextLayouts ??= CreateLabelTextLayouts();
        _sizeTextLayout ??= CreateSizeTextLayout();
        _downloadInfoTextLayout ??= CreateDownloadInfoTextLayout();
    }
    


    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        switch (change.Property.Name)
        {
            case nameof(Title):
            case nameof(SizeInBytes):
            case nameof(DetailItemsSource):
            case nameof(LabelItemsSource):
                InvalidateVisual();
                InvalidateMeasure();
                break;

            case nameof(Foreground):
            case nameof(LabelForeground):
            case nameof(StrongLabelForeground):
            case nameof(Background):
            case nameof(LabelBackground):
            case nameof(StrongLabelBackground):
                InvalidateVisual();
                break;
        }
    }
    */
}