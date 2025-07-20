// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using avallama.Models;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Media.TextFormatting;

namespace avallama.Controls;

// TODO:
// labeleknek háttér
// sizetext renderelése
// downloadstatus alapján kattintható svg renderelése
// új textlayout a letöltési információknak (Downloading.. 32%, Not enough space) stb.
// labelek ne nyúljanak túl a controlon, legyen új sorban ha nem fér ki, reszponzívan
// letöltési animációk, esetleg más animált megjelenés
// gradient hozzáadása a háttérhez (ha megoldható)
// command meghívása a paraméterével svgre kattintás esetén
// optimalizálás, control invalidálása csak akkor ha szükséges

public class ModelBlock : Control
{
    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<ModelBlock, string?>("Title");

    public static readonly StyledProperty<string> SizeTextProperty =
        AvaloniaProperty.Register<ModelBlock, string>("SizeText");

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

    public string SizeText
    {
        get => GetValue(SizeTextProperty);
        set => SetValue(SizeTextProperty, value);
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
    private const double ControlWidth = 260;
    private readonly Thickness _basePadding = new(12);
    private readonly CornerRadius _cornerRadius = new(12);
    private const string FontFamilyName = "Manrope";
    private const double TitleFontSize = 20;
    private const double DetailsFontSize = 12;
    private const double LabelFontSize = 10;
    private const double DetailsOpacity = 0.6;
    private const double DetailsLineHeight = 16;

    private TextLayout? _titleTextLayout;
    private TextLayout? _detailsTextLayout;
    private IEnumerable<TextLayout>? _labelTextLayouts;
    private TextLayout? _sizeTextLayout;

    public override void Render(DrawingContext context)
    {
        // Háttér renderelése
        RenderBackground(context);
        
        // Szövegek renderelése
        RenderText(context);
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
        
        // TODO: labelek háttér renderelés
    }

    private void RenderText(DrawingContext context)
    {
        // Ezek hardcoded értékek, meg van szabva hogy melyik elem között mennyi hely legyen
        // Mivel egyelőre nincs szükség propertyben megadva ezért így csináltam
        
        if (_titleTextLayout == null) return;
        var startingYPos = _basePadding.Top;
        _titleTextLayout.Draw(context, new Point(_basePadding.Left, startingYPos));
        startingYPos += _titleTextLayout.Height + _basePadding.Top / 1.5;
        if (_detailsTextLayout != null)
        {
            _detailsTextLayout.Draw(context, new Point(_basePadding.Left, startingYPos));
            startingYPos += _detailsTextLayout.Height + _basePadding.Top / 1.5;
        }

        if (_labelTextLayouts == null) return;
        var labelsWidth = 0.0;
        foreach (var labelTextLayout in _labelTextLayouts)
        {
            labelTextLayout.Draw(context, new Point(_basePadding.Left + labelsWidth, startingYPos));
            labelsWidth += labelTextLayout.Width + _basePadding.Left;
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
        if (DetailItemsSource == null) return null;

        // egy stringbe illeszti az összes detail itemet hogy egy textlayout elemként legyen lerenderelve
        var mergedDetailsText = string.Empty;
        var itemCount = 0;
        foreach (var detailItem in DetailItemsSource)
        {
            mergedDetailsText += $"{detailItem.Key}: {detailItem.Value}";
            if (itemCount != DetailItemsSource.Count - 1)
            {
                mergedDetailsText += "\n";
            }

            itemCount++;
        }

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

    private IEnumerable<TextLayout>? CreateLabelTextLayouts()
    {
        if (LabelItemsSource is not IEnumerable<ModelLabel> labels)
            return null;

        var labelList = labels as IList<ModelLabel> ?? labels.ToList();
        if (labelList.Count == 0)
            return null;

        List<TextLayout> layouts = [];
        layouts.AddRange(
            labelList.Select(
                label => new TextLayout(
                    label.Name, 
                    new Typeface(FontFamilyName), 
                    LabelFontSize, 
                    label.Highlight == ModelLabelHighlight.Default ? LabelForeground : StrongLabelForeground
                )
            )
        );

        return layouts;
    }
    
    private TextLayout? CreateSizeTextLayout()
    {
        if (string.IsNullOrEmpty(SizeText)) return null;

        return new TextLayout(
            SizeText,
            new Typeface(FontFamilyName),
            null,
            DetailsFontSize,
            new SolidColorBrush(
                (Foreground as ImmutableSolidColorBrush)?.Color ?? Colors.Black,
                DetailsOpacity
            )
        );
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
        base.OnMeasureInvalidated();
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        _titleTextLayout ??= CreateTitleTextLayout();
        _detailsTextLayout ??= CreateDetailsTextLayout();
        _labelTextLayouts ??= CreateLabelTextLayouts();
        _sizeTextLayout ??= CreateSizeTextLayout();
        
        var paddingHeight = _basePadding.Top + _basePadding.Bottom;
        var innerPaddingHeight = _basePadding.Top * 2 / 1.5;
        var textLayoutsHeight = _titleTextLayout?.Height + 
            _detailsTextLayout?.Height +
            (_labelTextLayouts as IList<TextLayout>)?[0].Height;
        
        return new Size(
            ControlWidth,
            paddingHeight + innerPaddingHeight + textLayoutsHeight ?? 0.0
        );
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        
        switch (change.Property.Name)
        {
            case nameof(Title):
            case nameof(SizeText):
            case nameof(DownloadStatus):
            case nameof(DownloadProgress):
            case nameof(DetailItemsSource):
            case nameof(LabelItemsSource):
            case nameof(Background):
            case nameof(Foreground):
            case nameof(LabelBackground):
            case nameof(LabelForeground):
            case nameof(StrongLabelBackground):
            case nameof(StrongLabelForeground):
                // TODO: megfelelően elhelyezni ezeket majd és optimalizálni a megjelenést
                InvalidateVisual();
                InvalidateMeasure();
                InvalidateArrange();
                break;
        }
    }
}