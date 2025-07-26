// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using avallama.Models;
using avallama.Services;
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

// TODO megvalósításra vár:
// downloadstatus alapján kattintható svg renderelése
// új textlayout a letöltési információknak (Downloading.. 32%, Not enough space) stb.
// letöltési animációk, esetleg más animált megjelenés
// gradient hozzáadása a háttérhez (ha megoldható)
// command meghívása a paraméterével svgre kattintás esetén
// optimalizálás, control invalidálása megfelelően, svg cachelése stb.
// strong kiemelt labelek legyenek utoljára kirenderelve

// TODO hibák:
// a Control összehúzásánál a labelek reszponzívak de a Bounds.Height-ja nem növekszik tehát a Control alá esnek be a labelek új sorban
// valamit kezdeni azzal hogy sok label esetén ne legyen olyan nagy az elem magasság vagy scrollable legyen idk
// változtatni a labelek háttereinek megjelenésén hogy ne tűnjenek úgy mintha gombok lennének
// különböző felbontásokon a lehető legjobban jelenjenek meg a szövegek

public class ModelBlock : Control
{
    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<ModelBlock, string?>("Title");

    public static readonly StyledProperty<double> SizeInBytesProperty =
        AvaloniaProperty.Register<ModelBlock, double>("SizeInBytes");

    public static readonly StyledProperty<ModelDownloadStatus> DownloadStatusProperty =
        AvaloniaProperty.Register<ModelBlock, ModelDownloadStatus>("DownloadStatus");

    public static readonly StyledProperty<double?> DownloadProgressProperty =
        AvaloniaProperty.Register<ModelBlock, double?>("DownloadProgress");

    public static readonly StyledProperty<IDictionary<string, string>?> DetailItemsSourceProperty =
        AvaloniaProperty.Register<ModelBlock, IDictionary<string, string>?>("DetailItemsSource");

    public static readonly StyledProperty<IEnumerable?> LabelItemsSourceProperty =
        AvaloniaProperty.Register<ModelBlock, IEnumerable?>("LabelItemsSource");

    public static readonly StyledProperty<ICommand> CommandProperty =
        AvaloniaProperty.Register<ModelBlock, ICommand>("Command");

    public static readonly StyledProperty<object?> CommandParameterProperty =
        AvaloniaProperty.Register<ModelBlock, object?>("CommandParameter");

    // tényleges stílus propertyk
    public static readonly StyledProperty<IBrush?> BackgroundProperty =
        AvaloniaProperty.Register<ModelBlock, IBrush?>("Background");

    public static readonly StyledProperty<IBrush?> ForegroundProperty =
        AvaloniaProperty.Register<ModelBlock, IBrush?>("Foreground");

    public static readonly StyledProperty<IBrush?> LabelBackgroundProperty =
        AvaloniaProperty.Register<ModelBlock, IBrush?>("LabelBackground");

    public static readonly StyledProperty<IBrush?> LabelForegroundProperty =
        AvaloniaProperty.Register<ModelBlock, IBrush?>("LabelForeground");

    public static readonly StyledProperty<IBrush?> StrongLabelBackgroundProperty =
        AvaloniaProperty.Register<ModelBlock, IBrush?>("StrongLabelBackground");

    public static readonly StyledProperty<IBrush?> StrongLabelForegroundProperty =
        AvaloniaProperty.Register<ModelBlock, IBrush?>("StrongLabelForeground");


    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public double SizeInBytes
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

    public IDictionary<string, string>? DetailItemsSource
    {
        get => GetValue(DetailItemsSourceProperty);
        set => SetValue(DetailItemsSourceProperty, value);
    }

    public IEnumerable? LabelItemsSource
    {
        get => GetValue(LabelItemsSourceProperty);
        set => SetValue(LabelItemsSourceProperty, value);
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

    public IBrush? Background
    {
        get => GetValue(BackgroundProperty);
        set => SetValue(BackgroundProperty, value);
    }

    public IBrush? Foreground
    {
        get => GetValue(ForegroundProperty);
        set => SetValue(ForegroundProperty, value);
    }

    public IBrush? LabelBackground
    {
        get => GetValue(LabelBackgroundProperty);
        set => SetValue(LabelBackgroundProperty, value);
    }

    public IBrush? LabelForeground
    {
        get => GetValue(LabelForegroundProperty);
        set => SetValue(LabelForegroundProperty, value);
    }

    public IBrush? StrongLabelBackground
    {
        get => GetValue(StrongLabelBackgroundProperty);
        set => SetValue(StrongLabelBackgroundProperty, value);
    }

    public IBrush? StrongLabelForeground
    {
        get => GetValue(StrongLabelForegroundProperty);
        set => SetValue(StrongLabelForegroundProperty, value);
    }

    // egyelőre égetett értékekkel, ha később igény lenne rá akkor külön styledpropertyre átvihetőek
    // ha változtatni szeretnél a megjelenésen akkor itt lehet leginkább
    private const double ControlWidth = 360;

    // alap padding a ModelBlockon belül
    private static readonly Thickness BasePadding = new(14);

    // alap padding a labelen belül
    private static readonly Thickness LabelPadding = new(8.5, 4.5);

    // labelek közti margin
    private static readonly Thickness LabelMargin = new(8);

    // a háttér corner radiusa
    private readonly CornerRadius _cornerRadius = new(12);

    // a label hátterének corner radiusa
    private readonly CornerRadius _labelCornerRadius = new(10);

    // üres hely nagysága a sizetext és a labelek között
    private const double SizeTextSpacing = 32;

    // Title alatt lévő padding
    private readonly double _titleBottomPadding = BasePadding.Bottom / 3;

    // Details alatt lévő padding
    private readonly double _detailsBottomPadding = BasePadding.Bottom / 1.5;

    private const string FontFamilyName = "Manrope";
    private const double TitleFontSize = 24;
    private const double DetailsFontSize = 14;
    private const double SizeFontSize = 16;
    private const double LabelFontSize = 10;
    private const double DownloadInfoFontSize = 14;
    private const double DetailsOpacity = 0.75;
    private const double DownloadInfoOpacity = 0.75;
    private const double SizeOpacity = 0.8;
    private const double DetailsLineHeight = 16;

    private const string DownloadSvgPath = "Assets/Svg/download.svg";
    private const string TrashSvgPath = "Assets/Svg/trash.svg";
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

    private double _labelsTotalHeight;

    public override void Render(DrawingContext context)
    {
        // Háttér renderelése
        RenderBackground(context);

        // Szövegek és labelek renderelése
        RenderBaseText(context);
        RenderLabels(context);

        // SVG gomb és szöveg (ha van) renderelése
        RenderStatus(context);
    }

    private void RenderBackground(DrawingContext context)
    {
        // TODO: gradientet hozzáadni?
        context.DrawRectangle(Background, null,
            new RoundedRect(
                new Rect(Bounds.Size),
                _cornerRadius
            )
        );
    }

    private void RenderBaseText(DrawingContext context)
    {
        // Ezek hardcoded értékek, meg van szabva hogy melyik elem között mennyi hely legyen
        // Mivel egyelőre nincs szükség propertyben megadva ezért így csináltam

        if (_titleTextLayout == null) return;
        _renderYPos = BasePadding.Top;
        _titleTextLayout.Draw(context, new Point(BasePadding.Left, _renderYPos));
        _renderYPos += _titleTextLayout.Height + _titleBottomPadding;
        if (_detailsTextLayout != null)
        {
            _detailsTextLayout.Draw(context, new Point(BasePadding.Left, _renderYPos));
            _renderYPos += _detailsTextLayout.Height + _detailsBottomPadding;
        }

        if (_sizeTextLayout == null) return;
        var sizeTextPos = new Point(
            Bounds.Width - _sizeTextLayout.Width - BasePadding.Right,
            Bounds.Height - _sizeTextLayout.Height - BasePadding.Bottom
        );

        _sizeTextLayout.Draw(context, sizeTextPos);
    }

    private void RenderLabels(DrawingContext context)
    {
        if (LabelItemsSource is not IEnumerable<ModelLabel> labelItemsEnum ||
            _labelTextLayouts is not { } labelLayoutsEnum)
        {
            return;
        }

        var labelItemsSource = labelItemsEnum.ToList();
        var labelTextLayouts = labelLayoutsEnum.ToList();

        _renderXPos = BasePadding.Left;
        _renderYPos += LabelPadding.Top;

        var sizeTextWidth = _sizeTextLayout?.Width + BasePadding.Right + BasePadding.Left + SizeTextSpacing;

        var remainingRowWidth = Bounds.Width - BasePadding.Left - sizeTextWidth - BasePadding.Right;

        for (var i = 0; i < labelItemsSource.Count; i++)
        {
            var labelBackground = labelItemsSource[i].Highlight == ModelLabelHighlight.Default
                ? LabelBackground
                : StrongLabelBackground;

            var labelBackgroundSize = new Size(
                labelTextLayouts[i].Width + LabelPadding.Left + LabelPadding.Right,
                labelTextLayouts[i].Height + LabelPadding.Top + LabelPadding.Bottom
            );

            // ha nem fér bele a meglévő sorba a label, akkor új sorba viszi
            if (remainingRowWidth < labelBackgroundSize.Width)
            {
                _renderXPos = BasePadding.Left;
                _renderYPos += LabelMargin.Bottom + labelBackgroundSize.Height;

                // elérhető sor szélességet visszaállítjuk, mert új soron lesz kirenderelve
                remainingRowWidth = Bounds.Width - BasePadding.Left - sizeTextWidth - BasePadding.Right;
            }

            remainingRowWidth -= labelBackgroundSize.Width + LabelMargin.Right;

            // label háttér kirenderelése
            context.DrawRectangle(labelBackground, null,
                new RoundedRect(
                    new Rect(
                        new Point(_renderXPos, _renderYPos),
                        labelBackgroundSize
                    ),
                    _labelCornerRadius
                )
            );

            // label szöveg kirenderelése
            labelTextLayouts[i].Draw(context,
                new Point(
                    _renderXPos + LabelPadding.Left,
                    _renderYPos + LabelPadding.Top
                )
            );

            _renderXPos += labelBackgroundSize.Width + LabelMargin.Right;
        }
    }

    private string? BrushToHex(IBrush? brush)
    {
        if (brush is not ImmutableSolidColorBrush solid) return null;
        var color = solid.Color;
        // 'X' azt jelenti hogy hexadecimális formátumban írja ki
        // és a 2-es hogy mindig két számjegyet használ, ha kell nullával előtölti
        // ha átlátszatlan, akkor #RRGGBB
        return color.A == 255
            ? $"#{color.R:X2}{color.G:X2}{color.B:X2}"
            :
            // ha van áttetszőség, akkor #AARRGGBB
            $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
    }

    private void RenderSvg(
        DrawingContext context, 
        string svgPath, 
        double svgScale,
        double xTranslation = 0.0,
        double yTranslation = 0.0
    )
    {
        // szín beállítása css-el, úgy hogy a Foreground propertyt átalakítjuk hex-re hogy css-nek át tudjuk adni
        var css = $"* {{ fill: {BrushToHex(Foreground)}; }}";
        var parameters = new SvgParameters(null, css);

        // svg betöltése
        using var stream = AssetLoader.Open(new Uri($"avares://avallama/{svgPath}"));
        _svgPicture = SvgSource.LoadPicture(stream, parameters);
        if (_svgPicture is null) return;
        _avaloniaSvgPicture = AvaloniaPicture.Record(_svgPicture);

        // svg skálázása
        var scaleMatrix = Matrix.CreateScale(svgScale, svgScale);

        // svg eltolása (0,0) pozíciótól a jobb felső sarokba figyelembe véve a skálázást is
        var translateMatrix = Matrix.CreateTranslation(
            Bounds.Width - BasePadding.Right - _svgPicture.CullRect.Width * svgScale + xTranslation,
            BasePadding.Top + yTranslation
        );

        // eltolások hozzáadása
        context.PushTransform(scaleMatrix * translateMatrix);
        _avaloniaSvgPicture.Draw(context);
    }

    private void RenderStatus(DrawingContext context)
    {
        // az xTranslation és yTranslation kisebb korrigálást jelent az svgkre, mert nem mindegyik van ugyanott, más méreteik vannak
        // bár még így is lehet hogy windowson, linuxon máshogy néz ki idk, ezt majd le kell ellenőrizni
        switch (DownloadStatus)
        {
            case ModelDownloadStatus.NotEnoughSpaceForDownload:
            case ModelDownloadStatus.NoConnectionForDownload:
                _downloadInfoTextLayout?.Draw(
                    context,
                    new Point(
                        Bounds.Width - _downloadInfoTextLayout.Width - BasePadding.Right,
                        BasePadding.Top
                    ));
                break;
            case ModelDownloadStatus.ReadyForDownload:
                RenderSvg(context, DownloadSvgPath, 1.15, xTranslation: +3.5, yTranslation: -3);
                break;
            case ModelDownloadStatus.Downloading:
                _downloadInfoTextLayout?.Draw(
                    context,
                    new Point(
                        Bounds.Width - _downloadInfoTextLayout.Width - BasePadding.Right * 3 -
                        (_svgPicture?.CullRect.Width ?? 0.0),
                        BasePadding.Top +
                        2.5 // kis korrekció hogy a "downloading.." szöveg egy vonalban legyen a pause svg-vel
                    ));
                RenderSvg(context, PauseSvgPath, 1.15, xTranslation: -4.5);
                break;
            case ModelDownloadStatus.Downloaded:
            default:
                RenderSvg(context, TrashSvgPath, 1);
                break;
        }
    }

    // TextLayoutok létrehozása
    private TextLayout? CreateTitleTextLayout()
    {
        if (string.IsNullOrEmpty(Title)) return null;

        return new TextLayout(
            Title,
            new Typeface(FontFamilyName),
            null,
            TitleFontSize,
            Foreground ?? Brushes.Black
        );
    }

    private TextLayout? CreateDetailsTextLayout()
    {
        if (DetailItemsSource == null || DetailItemsSource.Count == 0) return null;

        var mergedDetailsText = string.Join('\n', DetailItemsSource.Select(kv => $"{kv.Key}: {kv.Value}"));

        return new TextLayout(
            mergedDetailsText,
            new Typeface(FontFamilyName),
            null,
            DetailsFontSize,
            new SolidColorBrush(
                (Foreground as ImmutableSolidColorBrush)?.Color ?? Colors.Black,
                DetailsOpacity
            ),
            lineHeight: DetailsLineHeight
        );
    }

    private List<TextLayout>? CreateLabelTextLayouts()
    {
        if (LabelItemsSource is not IEnumerable<ModelLabel> labels) return null;

        var labelList = labels as IList<ModelLabel> ?? labels.ToList();
        if (labelList.Count == 0) return null;

        return labelList
            .Select(label => new TextLayout(
                label.Name,
                new Typeface(FontFamilyName),
                LabelFontSize,
                label.Highlight == ModelLabelHighlight.Default ? LabelForeground : StrongLabelForeground))
            .ToList();
    }

    private TextLayout? CreateSizeTextLayout()
    {
        if (double.IsNaN(SizeInBytes) || SizeInBytes < 0) return null;

        return new TextLayout(
            GetSizeInGb(),
            new Typeface(FontFamilyName),
            null,
            SizeFontSize,
            new SolidColorBrush(
                (Foreground as ImmutableSolidColorBrush)?.Color ?? Colors.Black,
                SizeOpacity
            )
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
                DownloadProgress?.ToString("0.00")),
            _ => string.Empty
        };

        return new TextLayout(
            downloadText,
            new Typeface(FontFamilyName),
            null,
            DownloadInfoFontSize,
            new SolidColorBrush(
                (Foreground as ImmutableSolidColorBrush)?.Color ?? Colors.Black,
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
            : rounded.ToString("0.0");

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
    protected override Size MeasureOverride(Size availableSize)
    {
        _titleTextLayout ??= CreateTitleTextLayout();
        _detailsTextLayout ??= CreateDetailsTextLayout();
        _labelTextLayouts ??= CreateLabelTextLayouts();
        _sizeTextLayout ??= CreateSizeTextLayout();
        _downloadInfoTextLayout ??= CreateDownloadInfoTextLayout();

        var contentHeight = 0.0;

        contentHeight += BasePadding.Top;

        if (_titleTextLayout is { } title)
        {
            contentHeight += title.Height;
            contentHeight += _titleBottomPadding;
        }

        if (_detailsTextLayout is { } details)
        {
            contentHeight += details.Height;
            contentHeight += _detailsBottomPadding;
        }

        _labelsTotalHeight = 0.0;

        if (_labelTextLayouts is List<TextLayout> { Count: > 0 } labelLayouts)
        {
            var currentRowWidth = 0.0;
            var currentRowHeight = 0.0;
            var totalLabelsHeight = 0.0;

            var sizeTextWidth = _sizeTextLayout?.Width + BasePadding.Right + BasePadding.Left + SizeTextSpacing;
            var maxRowWidth = ControlWidth - BasePadding.Left - sizeTextWidth - BasePadding.Right;

            foreach (var layout in labelLayouts)
            {
                var labelWidth = layout.Width + LabelPadding.Left + LabelPadding.Right;
                var labelHeight = layout.Height + LabelPadding.Top + LabelPadding.Bottom;

                if (currentRowWidth + labelWidth > maxRowWidth)
                {
                    totalLabelsHeight += currentRowHeight + LabelMargin.Bottom;
                    currentRowWidth = 0.0;
                    currentRowHeight = labelHeight;
                }
                else
                {
                    currentRowHeight = Math.Max(currentRowHeight, labelHeight);
                }

                currentRowWidth += labelWidth + LabelMargin.Right;
            }

            totalLabelsHeight += currentRowHeight;
            _labelsTotalHeight = totalLabelsHeight + LabelPadding.Top;
            contentHeight += _labelsTotalHeight;
        }

        // ha vannak labelek akkor a labelek alatti basepaddingot megadjuk
        if (_labelsTotalHeight > 0)
        {
            contentHeight += BasePadding.Bottom;
        }
        else
        {
            // ha pedig nincsenek akkor hozzáadjuk a basepaddingot de úgy hogy kivonjuk belőle a már eddigi hozzáadott paddinget
            // a második érték (_basePadding.Bottom / 1.5) ez egy padding érték ami a detailstext alá menne, de nyilván ha nincsenek labelek
            // akkor ezt a paddinget átírjuk úgy hogy egyezzen a sima base_paddingel
            contentHeight += BasePadding.Bottom - BasePadding.Bottom / 1.5;
        }

        return new Size(ControlWidth, contentHeight);
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
}