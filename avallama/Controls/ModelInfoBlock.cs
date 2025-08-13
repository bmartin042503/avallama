// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
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
// Learn more gomb kattinthatóvá tétele és átirányítás ollama oldalára (url-be betéve a model nevét)
// Letöltés, szüneteltetés, törlés UI
// noconnection és notenoughspace figyelmeztetések
// BorderThickness a kijelzőméret alapján, hogy a lehető legjobb megjelenése legyen
// szövegkijelölés, vmi új felhasználható típusban
// --------------------------------------------
// scrollviewerbe tenni a modelinformationt
// (ez még talán belefér későbbre, 4-6 sornyi infót még meg tud jeleníteni de ha ennél több van akkor valszeg ki fog nyúlni a szöveg alulra)
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
    private const double LabelFontSize = 12;
    private const double DetailsFontSize = 14;
    private const double DetailsLineHeight = 28;
    private const double BorderThickness = 0.25;
    private const double LabelSpacing = 8.0;

    private static readonly Thickness BasePadding = new(16);
    private static readonly Thickness LabelPadding = new(10, 5);
    private static readonly Thickness ContainerPadding = new(12);
    private static readonly CornerRadius BaseCornerRadius = new(10);
    private static readonly CornerRadius ContainerCornerRadius = new(8);
    private static readonly CornerRadius LabelCornerRadius = new(4);

    private TextLayout? _titleTextLayout;
    private TextLayout? _warningTextLayout;
    private TextLayout? _quantizationTextLayout;
    private TextLayout? _parametersTextLayout;
    private TextLayout? _formatTextLayout;
    private TextLayout? _sizeTextLayout;
    private TextLayout? _detailsTextLayout;
    private TextLayout? _modelInfoTextLayout;
    private TextLayout? _linkTextLayout;

    private double _renderPosX;
    private double _renderPosY;

    private double _labelsHeight;

    public override void Render(DrawingContext context)
    {
        // Háttér renderelése
        RenderBackground(context);

        // Model cím (+warning szöveg ha van) renderelése
        RenderTitle(context);

        // alap címkék renderelése (kvantálás, paraméterek, format) + méret label
        RenderLabels(context);

        // model információinak renderelése
        RenderModelInformation(context);
    }

    private void RenderBackground(DrawingContext context)
    {
        context.DrawRectangle(
            ColorProvider.GetColor(AppColor.SurfaceContainer),
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

        _renderPosY += BasePadding.Top + (_titleTextLayout?.Height ?? 0) + BasePadding.Bottom;
        _renderPosX = BasePadding.Left;
    }

    // kirajzol egy alap labelt, egy hátteret és egy benne lévő szöveget, _renderPosX és _renderPosY alapján
    // a három alap infónak van ami kiemelten jelenik meg
    private void DrawBaseLabel(
        DrawingContext context,
        double width,
        double height,
        TextLayout? textLayout
    )
    {
        if (textLayout == null) return;

        double posXChange;
        var labelHeight = LabelPadding.Top + textLayout.Height + LabelPadding.Bottom + LabelSpacing;

        // belefér a sorba a label
        if (Bounds.Width - BasePadding.Left - BasePadding.Right - _renderPosX >= textLayout.Width)
        {
            posXChange = LabelPadding.Left + textLayout.Width + LabelPadding.Right + LabelSpacing;
            _labelsHeight = Math.Max(_labelsHeight, labelHeight);
        }
        else
        {
            // ha új sorba kezdődik akkor reseteljük a renderPosX-et és hozzáadjuk a label magasságot a renderPosY-hoz
            posXChange = 0.0;
            _renderPosX = BasePadding.Left;
            _renderPosY += LabelPadding.Top + textLayout.Height + LabelPadding.Bottom + LabelSpacing;
            _labelsHeight += labelHeight;
        }

        context.DrawRectangle(
            ColorProvider.GetColor(AppColor.Secondary), null,
            new RoundedRect(
                new Rect(
                    new Point(
                        _renderPosX,
                        _renderPosY
                    ),
                    new Size(
                        width,
                        height
                    )
                ),
                LabelCornerRadius
            )
        );

        textLayout.Draw(
            context,
            new Point(_renderPosX + LabelPadding.Left, _renderPosY + LabelPadding.Top)
        );

        // a render pozíció megkapja az X változást (ha pl. nem került új sorba az adott label akkor hozzáadjuk annak a hosszát
        // egyébként meg nullát, ha új sorba került és akkor megint BasePadding.Left-ről kezdi
        _renderPosX += posXChange;
    }

    private void RenderLabels(DrawingContext context)
    {
        if (_quantizationTextLayout != null && Quantization.HasValue)
        {
            DrawBaseLabel(context,
                LabelPadding.Left + _quantizationTextLayout.Width + LabelPadding.Right,
                LabelPadding.Top + _quantizationTextLayout.Height + LabelPadding.Bottom,
                _quantizationTextLayout
            );
        }

        if (_parametersTextLayout != null && Parameters.HasValue)
        {
            DrawBaseLabel(context,
                LabelPadding.Left + _parametersTextLayout.Width + LabelPadding.Right,
                LabelPadding.Top + _parametersTextLayout.Height + LabelPadding.Bottom,
                _parametersTextLayout
            );
        }

        if (_formatTextLayout != null && !string.IsNullOrEmpty(Format))
        {
            DrawBaseLabel(context,
                LabelPadding.Left + _formatTextLayout.Width + LabelPadding.Right,
                LabelPadding.Top + _formatTextLayout.Height + LabelPadding.Bottom,
                _formatTextLayout
            );
        }

        // renderPosY újra számítása, ugyanis itt nem lehetne szimplán hozzáadni a labelsHeightot mert a renderelés közben már
        // hozzá lett adva, nem az egész, de így könnyebb ha újraszámoljuk hogy hol kell lennie a render pozíciónak
        _renderPosY = BasePadding.Top + (_titleTextLayout?.Height ?? 0.0) + BasePadding.Bottom + _labelsHeight -
            LabelSpacing + BasePadding.Bottom;

        // SizeLabel (ehhez nem kell renderPosX és renderPosY mert fixen mindig a bal alsó sarokban lesz)
        if (_sizeTextLayout == null) return;
        context.DrawRectangle(
            ColorProvider.GetColor(AppColor.SurfaceContainerHighest),
            new Pen(ColorProvider.GetColor(AppColor.OnSurface), BorderThickness),
            new RoundedRect(
                new Rect(
                    new Point(
                        BasePadding.Left,
                        Bounds.Height - BasePadding.Bottom - LabelPadding.Bottom - _sizeTextLayout.Height -
                        LabelPadding.Top
                    ),
                    new Size(
                        width: LabelPadding.Left + _sizeTextLayout.Width + LabelPadding.Right,
                        height: LabelPadding.Top + _sizeTextLayout.Height + LabelPadding.Bottom
                    )
                ),
                LabelCornerRadius
            )
        );

        _sizeTextLayout.Draw(
            context,
            new Point(
                BasePadding.Left + LabelPadding.Left,
                Bounds.Height - BasePadding.Bottom - LabelPadding.Bottom - _sizeTextLayout.Height
            )
        );
    }

    private void RenderModelInformation(DrawingContext context)
    {
        // "model information" szöveg és a details text közötti térköz
        const double spacing = 14.0;

        context.DrawRectangle(
            ColorProvider.GetColor(AppColor.SurfaceContainerHighest),
            new Pen(ColorProvider.GetColor(AppColor.OnSurface), BorderThickness),
            new RoundedRect(
                new Rect(
                    new Point(
                        BasePadding.Left,
                        _renderPosY
                    ),
                    new Size(
                        width: Bounds.Width - BasePadding.Left - BasePadding.Right,
                        height: Bounds.Height - _renderPosY - BasePadding.Bottom * 3 - LabelPadding.Top -
                                (_sizeTextLayout?.Height ?? 0.0) - LabelPadding.Bottom
                    )
                ),
                ContainerCornerRadius
            )
        );

        _renderPosY += ContainerPadding.Top;
        _renderPosX = BasePadding.Left + ContainerPadding.Left;
        
        _modelInfoTextLayout?.Draw(
            context,
            new Point(
                _renderPosX,
                _renderPosY
            )
        );

        _renderPosY += (_modelInfoTextLayout?.Height ?? 0) + spacing;

        _detailsTextLayout?.Draw(
            context,
            new Point(
                _renderPosX,
                _renderPosY
            )
        );

        _linkTextLayout?.Draw(
            context,
            new Point(
                BasePadding.Left + ContainerPadding.Left,
                Bounds.Height - BasePadding.Bottom * 3 - LabelPadding.Bottom - (_sizeTextLayout?.Height ?? 0.0) -
                LabelPadding.Top - ContainerPadding.Bottom - _linkTextLayout.Height
            )
        );
    }

    // Model címéhez tartozó textlayout létrehozása
    private TextLayout? CreateTitleTextLayout()
    {
        if (string.IsNullOrEmpty(Title)) return null;

        const double spacing = 26.0;

        return new TextLayout(
            Title,
            new Typeface(RenderHelper.ManropeFont),
            null,
            TitleFontSize,
            ColorProvider.GetColor(AppColor.OnSurface),
            maxWidth: Bounds.Width - BasePadding.Left - BasePadding.Right - (_warningTextLayout?.Width + spacing ?? 0),
            textWrapping: TextWrapping.Wrap,
            maxLines: 2,
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

    private TextLayout? CreateSizeTextLayout()
    {
        if (!SizeInBytes.HasValue) return null;

        var formattedSize = string.Format(LocalizationService.GetString("MODEL_SIZE"),
            RenderHelper.GetSizeInGb(SizeInBytes.Value));

        return new TextLayout(
            formattedSize,
            new Typeface(RenderHelper.ManropeFont, weight: FontWeight.Regular),
            null,
            LabelFontSize,
            ColorProvider.GetColor(AppColor.OnSurface)
        );
    }

    private TextLayout CreateModelInfoTextLayout()
    {
        return new TextLayout(
            LocalizationService.GetString("MODEL_INFORMATION"),
            new Typeface(RenderHelper.ManropeFont, weight: FontWeight.Bold),
            null,
            DetailsFontSize,
            ColorProvider.GetColor(AppColor.OnSurface)
        );
    }

    private TextLayout CreateLinkTextLayout()
    {
        return new TextLayout(
            LocalizationService.GetString("LEARN_MORE"),
            new Typeface(RenderHelper.ManropeFont, weight: FontWeight.Bold),
            fontFeatures: null,
            textDecorations:
            [
                new TextDecoration
                {
                    Location = TextDecorationLocation.Underline,
                    Stroke = ColorProvider.GetColor(AppColor.Primary),
                    StrokeThickness = 1,
                    StrokeOffset = 0.75,
                    StrokeThicknessUnit = TextDecorationUnit.Pixel,
                    StrokeOffsetUnit = TextDecorationUnit.Pixel
                }
            ],
            fontSize: DetailsFontSize,
            foreground: ColorProvider.GetColor(AppColor.Primary)
        );
    }

    // controlhoz tartozó layoutok stb. felszabadítása
    private void InvalidateTextLayouts()
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
        _sizeTextLayout?.Dispose();
        _sizeTextLayout = null;
        _modelInfoTextLayout?.Dispose();
        _modelInfoTextLayout = null;
        _linkTextLayout?.Dispose();
        _linkTextLayout = null;

        _renderPosX = 0;
        _renderPosY = 0;

        _labelsHeight = 0;
    }

    private void CreateTextLayouts()
    {
        // ennek itt kell lennie legelöl, hogy a titleTextLayout tudja mekkora helyet foglal el a warning szöveg
        // és annak alapján fogja ő is kitölteni a helyet
        _warningTextLayout ??= CreateWarningTextLayout();

        _titleTextLayout ??= CreateTitleTextLayout();
        _detailsTextLayout ??= CreateDetailsTextLayout();
        _sizeTextLayout ??= CreateSizeTextLayout();
        _modelInfoTextLayout ??= CreateModelInfoTextLayout();
        _linkTextLayout ??= CreateLinkTextLayout();

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
        InvalidateTextLayouts();
        base.OnMeasureInvalidated();
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        CreateTextLayouts();

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
                InvalidateTextLayouts();
                InvalidateVisual();
                CreateTextLayouts();
                break;
            case nameof(Bounds):
                InvalidateMeasure();
                break;
        }
    }
}