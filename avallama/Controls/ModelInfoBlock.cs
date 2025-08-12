// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using avallama.Constants;
using avallama.Models;
using avallama.Services;
using avallama.Utilities;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;

namespace avallama.Controls;

// TODO:
// BorderThickness a kijelzőméret alapján, hogy a lehető legjobb megjelenése legyen
// szövegkijelölés, vmi új felhasználható típusban
public class ModelInfoBlock : Control
{
    // StyledProperty - belekerül az Avalonia styles rendszerébe így például írhatunk rá stílusokat stb.
    // DirectProperty - nem kerül bele, megadott egyszerű értékeknek, jobb teljesítmény (kell getter, setter)

    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<ModelInfoBlock, string?>(nameof(Title));

    public static readonly DirectProperty<ModelInfoBlock, int?> QuantizationProperty =
        AvaloniaProperty.RegisterDirect<ModelInfoBlock, int?>(
            nameof(Quantization),
            o => o.Quantization,
            (o, v) => o.Quantization = v
        );

    public static readonly DirectProperty<ModelInfoBlock, double?> ParametersProperty =
        AvaloniaProperty.RegisterDirect<ModelInfoBlock, double?>(
            nameof(Parameters),
            o => o.Parameters,
            (o, v) => o.Parameters = v
        );

    public static readonly DirectProperty<ModelInfoBlock, string?> FormatProperty =
        AvaloniaProperty.RegisterDirect<ModelInfoBlock, string?>(
            nameof(Format),
            o => o.Format,
            (o, v) => o.Format = v
        );

    public static readonly DirectProperty<ModelInfoBlock, IDictionary<string, string>?> DetailItemsSourceProperty =
        AvaloniaProperty.RegisterDirect<ModelInfoBlock, IDictionary<string, string>?>(
            nameof(DetailItemsSource),
            o => o.DetailItemsSource,
            (o, v) => o.DetailItemsSource = v
        );

    public static readonly DirectProperty<ModelInfoBlock, long?> SizeInBytesProperty =
        AvaloniaProperty.RegisterDirect<ModelInfoBlock, long?>(
            nameof(SizeInBytes),
            o => o.SizeInBytes,
            (o, v) => o.SizeInBytes = v
        );

    public static readonly DirectProperty<ModelInfoBlock, ModelDownloadStatus?> DownloadStatusProperty =
        AvaloniaProperty.RegisterDirect<ModelInfoBlock, ModelDownloadStatus?>(
            nameof(DownloadStatus),
            o => o.DownloadStatus,
            (o, v) => o.DownloadStatus = v
        );

    public static readonly DirectProperty<ModelInfoBlock, double?> DownloadProgressProperty =
        AvaloniaProperty.RegisterDirect<ModelInfoBlock, double?>(
            nameof(DownloadProgress),
            o => o.DownloadProgress,
            (o, v) => o.DownloadProgress = v
        );

    public static readonly DirectProperty<ModelInfoBlock, bool?> RunsSlowProperty =
        AvaloniaProperty.RegisterDirect<ModelInfoBlock, bool?>(
            nameof(RunsSlow),
            o => o.RunsSlow,
            (o, v) => o.RunsSlow = v
        );

    public static readonly StyledProperty<ICommand> CommandProperty =
        AvaloniaProperty.Register<ModelInfoBlock, ICommand>(nameof(Command));

    public static readonly StyledProperty<object?> CommandParameterProperty =
        AvaloniaProperty.Register<ModelInfoBlock, object?>(nameof(CommandParameter));


    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    private int? _quantization;

    public int? Quantization
    {
        get => _quantization;
        set => SetAndRaise(QuantizationProperty, ref _quantization, value);
    }

    private double? _parameters;

    public double? Parameters
    {
        get => _parameters;
        set => SetAndRaise(ParametersProperty, ref _parameters, value);
    }

    private string? _format;

    public string? Format
    {
        get => _format;
        set => SetAndRaise(FormatProperty, ref _format, value);
    }

    private IDictionary<string, string>? _detailItemsSource;

    public IDictionary<string, string>? DetailItemsSource
    {
        get => _detailItemsSource;
        set => SetAndRaise(DetailItemsSourceProperty, ref _detailItemsSource, value);
    }

    private long? _sizeInBytes;

    public long? SizeInBytes
    {
        get => _sizeInBytes;
        set => SetAndRaise(SizeInBytesProperty, ref _sizeInBytes, value);
    }

    private ModelDownloadStatus? _downloadStatus;

    public ModelDownloadStatus? DownloadStatus
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

    private bool? _runsSlow;

    public bool? RunsSlow
    {
        get => _runsSlow;
        set => SetAndRaise(RunsSlowProperty, ref _runsSlow, value);
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
    
    private const double TitleFontSize = 26;
    private const double WarningFontSize = 14;
    private const double LabelFontSize = 14;
    private const double DetailsFontSize = 12;
    private const double DetailsLineHeight = 16;
    private const double BorderThickness = 0.5;

    private static readonly Thickness BasePadding = new(16);
    private static readonly CornerRadius BaseCornerRadius = new(10);

    private TextLayout? _titleTextLayout;
    private TextLayout? _warningTextLayout;
    private TextLayout? _quantizationTextLayout;
    private TextLayout? _parametersTextLayout;
    private TextLayout? _formatTextLayout;
    private TextLayout? _detailsTextLayout;

    // private static double RenderPosX;
    // private static double RenderPosY;

    public override void Render(DrawingContext context)
    {
        // Háttér renderelése
        RenderBackground(context);
        
        // Model cím (+warning szöveg ha van) renderelés
        RenderTitle(context);
    }

    private void RenderBackground(DrawingContext context)
    {
        context.DrawRectangle(
            ColorProvider.GetColor(AppColor.SurfaceContainerHigh),
            new Pen(ColorProvider.GetColor(AppColor.OnSurface), BorderThickness),
            new RoundedRect(
                new Rect(Bounds.Size),
                BaseCornerRadius
            )
        );
    }

    private void RenderTitle(DrawingContext context)
    {
        _titleTextLayout?.Draw(
            context,
            new Point(BasePadding.Left, BasePadding.Top)
        );

        const double spacing = 6.0;
        
        _warningTextLayout?.Draw(
            context,
            new Point(
                Bounds.Width - _warningTextLayout.Width - BasePadding.Right, 
                BasePadding.Top + spacing
            )
        );
    }

    // Model címéhez tartozó textlayout létrehozása
    private TextLayout? CreateTitleTextLayout()
    {
        if (string.IsNullOrEmpty(Title)) return null;

        const double spacing = 18.0;

        return new TextLayout(
            Title,
            new Typeface(RenderHelper.ManropeFont),
            null,
            TitleFontSize,
            ColorProvider.GetColor(AppColor.OnSurface),
            maxWidth: Bounds.Width - BasePadding.Left - BasePadding.Right - (_warningTextLayout?.Width + spacing ?? 0),
            textWrapping: TextWrapping.Wrap,
            maxLines: 3,
            textTrimming: TextTrimming.CharacterEllipsis
        );
    }
    
    // Warning textlayout létrehozása (ha szükség van rá, pl. ki kell írni hogy lassan futhat a model)
    private TextLayout? CreateWarningTextLayout()
    {
        if (!RunsSlow.HasValue || !RunsSlow.Value) return null;
        return new TextLayout(
            LocalizationService.GetString("MIGHT_RUN_SLOW"),
            new Typeface(RenderHelper.ManropeFont, weight: FontWeight.Regular),
            null,
            WarningFontSize,
            ColorProvider.GetColor(AppColor.Error)
        );
    }

    // quantizationnek, parametersnek és a formatnak
    private TextLayout CreateLabelTextLayout(string text)
    {
        return new TextLayout(
            text,
            new Typeface(RenderHelper.ManropeFont, weight: FontWeight.Bold),
            null,
            LabelFontSize,
            ColorProvider.GetColor(AppColor.OnSecondary)
        );
    }
    
    // Model részleteihez tartozó textlayout létrehozása
    private TextLayout? CreateDetailsTextLayout()
    {
        if (DetailItemsSource == null || DetailItemsSource.Count == 0) return null;

        // details elemek egy stringbe fűzése
        var mergedDetailsText = string.Join('\n', DetailItemsSource.Select(kv => $"{kv.Key}: {kv.Value}"));

        return new TextLayout(
            mergedDetailsText,
            new Typeface(RenderHelper.ManropeFont),
            null,
            DetailsFontSize,
            ColorProvider.GetColor(AppColor.OnSurface),
            lineHeight: DetailsLineHeight
        );
    }
    
    // controlhoz tartozó layoutok stb. felszabadítása
    private void InvalidateModelInfo()
    {
        _warningTextLayout?.Dispose();
        _warningTextLayout = null;
        _titleTextLayout?.Dispose();
        _titleTextLayout = null;
        _detailsTextLayout?.Dispose();
        _detailsTextLayout = null;
        _quantizationTextLayout?.Dispose();
        _quantizationTextLayout = null;
        _parametersTextLayout?.Dispose();
        _parametersTextLayout = null;
        _formatTextLayout?.Dispose();
        _formatTextLayout = null;
    }

    private void CreateModelInfo()
    {
        // ennek itt kell lennie legelöl, hogy a titleTextLayout tudja mekkora helyet foglal el a warning szöveg
        // és annak alapján fogja ő is kitölteni a helyet
        _warningTextLayout ??= CreateWarningTextLayout();
        
        _titleTextLayout ??= CreateTitleTextLayout();
        _detailsTextLayout ??= CreateDetailsTextLayout();
        
        if (Quantization is > 0)
        {
            _quantizationTextLayout ??= CreateLabelTextLayout(
                string.Format(LocalizationService.GetString("MODEL_QUANTIZATION"), Quantization)    
            );
        }

        if (Parameters is > 0)
        {
            _parametersTextLayout ??= CreateLabelTextLayout(
                string.Format(LocalizationService.GetString("MODEL_PARAMETER_SIZE"), Parameters)    
            );
        }

        if (!string.IsNullOrEmpty(Format))
        {
            _formatTextLayout ??= CreateLabelTextLayout(Format);
        }
    }
    
    protected override void OnMeasureInvalidated()
    {
        InvalidateModelInfo();
        base.OnMeasureInvalidated();
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        CreateModelInfo();

        // visszaadjuk a teljes elérhető szélességet, magasságot, hiszen ez a control kitölti a teljes rendelkezésre álló helyet
        return new Size(
            width: availableSize.Width,
            height: availableSize.Height
        );
    }
    
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        switch (change.Property.Name)
        {
            case nameof(Title):
            case nameof(Quantization):
            case nameof(Parameters):
            case nameof(Format):
            case nameof(DetailItemsSource):
            case nameof(SizeInBytes):
            case nameof(DownloadStatus):
            case nameof(DownloadProgress):
            case nameof(RunsSlow):
                InvalidateModelInfo();
                InvalidateVisual();
                CreateModelInfo();
                break;
            case nameof(Bounds):
                InvalidateMeasure();
                break;
                
        }
    }
}