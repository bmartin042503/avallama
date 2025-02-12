﻿using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;

namespace avallama.Controls;

/// <summary>
/// Szövegdoboz állítható háttérrel, két szöveggel, személyre szabott propertykkel
/// </summary>
public class MessageBlock : Control
{
    // AXAML Styled Propertyk
    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<MessageBlock, string?>("Text");
    
    public static readonly StyledProperty<IBrush?> TextColorProperty =
        AvaloniaProperty.Register<MessageBlock, IBrush?>("TextColor");
    
    public static readonly StyledProperty<string?> SubTextProperty =
        AvaloniaProperty.Register<MessageBlock, string?>("SubText");
    
    public static readonly StyledProperty<IBrush?> SubTextColorProperty =
        AvaloniaProperty.Register<MessageBlock, IBrush?>("SubTextColor");
    
    public static readonly StyledProperty<Thickness?> PaddingProperty =
        AvaloniaProperty.Register<MessageBlock, Thickness?>("Padding");
    
    public static readonly StyledProperty<CornerRadius?> CornerRadiusProperty =
        AvaloniaProperty.Register<MessageBlock, CornerRadius?>("CornerRadius");
    
    public static readonly StyledProperty<double?> TextFontSizeProperty =
        AvaloniaProperty.Register<MessageBlock, double?>("TextFontSize");
    
    public static readonly StyledProperty<double?> SubTextFontSizeProperty =
        AvaloniaProperty.Register<MessageBlock, double?>("SubTextFontSize");
    
    public static readonly StyledProperty<TextAlignment?> TextAlignmentProperty =
        AvaloniaProperty.Register<MessageBlock, TextAlignment?>("TextAlignment");
    
    public static readonly StyledProperty<TextAlignment?> SubTextAlignmentProperty =
        AvaloniaProperty.Register<MessageBlock, TextAlignment?>("SubTextAlignment");
    
    public static readonly StyledProperty<FontFamily?> FontFamilyProperty =
        AvaloniaProperty.Register<MessageBlock, FontFamily?>("FontFamily");
    
    public static readonly StyledProperty<IBrush?> BackgroundProperty =
        AvaloniaProperty.Register<MessageBlock, IBrush?>("Background");
    
    public static readonly StyledProperty<double?> SpacingProperty =
        AvaloniaProperty.Register<MessageBlock, double?>("Spacing");
    
    public static readonly StyledProperty<string?> UnitProperty =
        AvaloniaProperty.Register<MessageBlock, string?>("Unit");
    
    public static readonly StyledProperty<double?> LineHeightProperty =
        AvaloniaProperty.Register<MessageBlock, double?>("LineHeight");

    public static readonly StyledProperty<IBrush?> SelectionColorProperty =
        AvaloniaProperty.Register<MessageBlock, IBrush?>("SelectionColor");

    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public IBrush? TextColor
    {
        get => GetValue(TextColorProperty);
        set => SetValue(TextColorProperty, value);
    }

    public string? SubText
    {
        get => GetValue(SubTextProperty);
        set => SetValue(SubTextProperty, value);
    }

    public IBrush? SubTextColor
    {
        get => GetValue(SubTextColorProperty);
        set => SetValue(SubTextColorProperty, value);
    }

    public Thickness? Padding
    {
        get => GetValue(PaddingProperty);
        set => SetValue(PaddingProperty, value);
    }

    public CornerRadius? CornerRadius
    {
        get => GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }

    public double? TextFontSize
    {
        get => GetValue(TextFontSizeProperty);
        set => SetValue(TextFontSizeProperty, value);
    }

    public double? SubTextFontSize
    {
        get => GetValue(SubTextFontSizeProperty);
        set => SetValue(SubTextFontSizeProperty, value);
    }

    public TextAlignment? TextAlignment
    {
        get => GetValue(TextAlignmentProperty);
        set => SetValue(TextAlignmentProperty, value);
    }

    public TextAlignment? SubTextAlignment
    {
        get => GetValue(SubTextAlignmentProperty);
        set => SetValue(SubTextAlignmentProperty, value);
    }

    public FontFamily? FontFamily
    {
        get => GetValue(FontFamilyProperty);
        set => SetValue(FontFamilyProperty, value);
    }

    public IBrush? Background
    {
        get => GetValue(BackgroundProperty);
        set => SetValue(BackgroundProperty, value);
    }

    public double? Spacing
    {
        get => GetValue(SpacingProperty);
        set => SetValue(SpacingProperty, value);
    }

    public string? Unit
    {
        get => GetValue(UnitProperty);
        set => SetValue(UnitProperty, value);
    }

    public double? LineHeight
    {
        get => GetValue(LineHeightProperty);
        set => SetValue(LineHeightProperty, value);
    }

    public IBrush? SelectionColor
    {
        get => GetValue(SelectionColorProperty);
        set => SetValue(SelectionColorProperty, value);
    }
    
    private TextLayout? _textLayout;
    private TextLayout? _subTextLayout;

    // a MaxWidth és MaxHeight megfelelő beállításához kell a Create(Sub)TextLayoutnak
    private Size _constraint = Size.Infinity;

    public TextLayout? TextLayout => _textLayout ??= CreateTextLayout();
    public TextLayout? SubTextLayout => _subTextLayout ??= CreateSubTextLayout();

    private Point? _textLayoutPosition;
    
    public override void Render(DrawingContext context)
    {
        // Háttér renderelése
        RenderBackground(context);
        
        // Szövegek renderelése
        RenderText(context);
    }

    private void RenderBackground(DrawingContext context)
    {
        // Ha van háttér megadva akkor lerendereljük a CornerRadius alapján (ami lehet 0 is)
        var bg = Background;
        if (bg != null)
        {
            var cornerRadius = CornerRadius ?? new CornerRadius(0,0,0,0);
            context.DrawRectangle(bg, null,
                new RoundedRect(
                    new Rect(Bounds.Size),
                    cornerRadius.TopLeft,
                    cornerRadius.TopRight,
                    cornerRadius.BottomRight,
                    cornerRadius.BottomLeft
                )
            );
        }
    }

    // Létrehozott TextLayoutok renderelése (amennyiben nem null)
    private void RenderText(DrawingContext context)
    {
        TextLayout?.Draw(context, CalculateTextPosition(TextAlignment));
        SubTextLayout?.Draw(context, CalculateSubTextPosition(SubTextAlignment));
        /* szöveg piros border render debug
        if (TextLayout != null)
        {
            var heightDifference = 
                (TextLayout.TextLines[0].Height - TextLayout.TextLines[0].Baseline);
            for(int i = 0; i < TextLayout.TextLines.Count; i++)
            {
                double modifier = 0.0;
                if (i == 0)
                {
                    modifier = heightDifference;
                }
                else if(i == TextLayout.TextLines.Count - 1)
                {
                    modifier = heightDifference * -1;
                }
                var line = TextLayout.TextLines[i];
                var size = new Size(line.Width, line.Height);
                var pos = new Point(
                    _textLayoutPosition.Value.X, 
                    _textLayoutPosition.Value.Y+(i*line.Height)+modifier
                );
                var rect = new Rect(pos, size);
                context.DrawRectangle(null, new Pen(Brushes.Red, 2.0), rect);
            }
        }*/
    }

    // Létrehozza az alap szöveget (amennyiben meg van adva)
    private TextLayout? CreateTextLayout()
    {
        if (!string.IsNullOrEmpty(Text))
        {
            return new TextLayout(
                Text,
                new Typeface(FontFamily ?? FontFamily.Default),
                TextFontSize ?? 12,
                TextColor ?? Brushes.Black,
                TextAlignment ?? Avalonia.Media.TextAlignment.Center,
                TextWrapping.Wrap,
                null,
                null,
                FlowDirection.LeftToRight,
                _constraint.Width,
                _constraint.Height,
                LineHeight ?? double.NaN
            );
        }
        return null;
    }

    // Létrehozza az alsó szöveget (amennyiben meg van adva)
    private TextLayout? CreateSubTextLayout()
    {
        if (!string.IsNullOrEmpty(SubText))
        {
            return new TextLayout(
                $"{SubText} {Unit}",
                new Typeface(FontFamily ?? FontFamily.Default),
                SubTextFontSize ?? 8,
                SubTextColor ?? Brushes.Black,
                SubTextAlignment ?? Avalonia.Media.TextAlignment.Right,
                TextWrapping.Wrap,
                null,
                null,
                FlowDirection.LeftToRight,
                _constraint.Width,
                _constraint.Height
            );
        }

        return null;
    }

    // Invalidálja a vizuális elemeket és az elemek leméretezését, és egy újat kér helyette
    private void InvalidateTextLayouts()
    {
        InvalidateVisual();
        InvalidateMeasure();
    }

    protected override void OnMeasureInvalidated()
    {
        // felszabadítja a textLayoutokat
        _textLayout?.Dispose();
        _textLayout = null;
        _subTextLayout?.Dispose();
        _subTextLayout = null;
        base.OnMeasureInvalidated();
    }

    // Felméri hogy mennyi helyre van szüksége a Controlnak
    protected override Size MeasureOverride(Size availableSize)
    {
        var scale = LayoutHelper.GetLayoutScale(this);
        
        // LayoutHelperrel roundolja a Thicknesst (megadott Paddingot) magas dpi képernyőkre, a megfelelő koordinátákhoz
        var padding = LayoutHelper.RoundLayoutThickness(Padding ?? new Thickness(0,0,0,0), scale, scale);
        var deflatedSize = availableSize.Deflate(padding); // kiveszi az elérhető helyből a paddingot
        
        // ha a constraint nem egyezik akkor reseteli a textLayoutokat és újraigazítja őket
        if (_constraint != deflatedSize)
        {
            _textLayout?.Dispose();
            _textLayout = null;
            _subTextLayout?.Dispose();
            _subTextLayout = null;
            _constraint = deflatedSize;
            InvalidateArrange();
        }
        
        // implicit létrehozza az új textlayoutokat
        var textLayout = TextLayout;
        var subTextLayout = SubTextLayout;
        
        // a lehető legnagyobb szélesség a textlayoutokra nézve
        var textLayoutWidth = textLayout == null ? 0 : textLayout.OverhangLeading + textLayout.WidthIncludingTrailingWhitespace + textLayout.OverhangTrailing;
        var subTextLayoutWidth = subTextLayout == null ? 0 : subTextLayout.OverhangLeading + subTextLayout.WidthIncludingTrailingWhitespace + subTextLayout.OverhangTrailing;

        // a lehető legnagyobb hosszúság a textlayoutokra nézve
        var textLayoutHeight = textLayout?.Height ?? 0;
        var subTextLayoutHeight = subTextLayout?.Height ?? 0;

        double spacing;
        // ha valamelyik textlayout hiányzik a spacingot 0-ra állítjuk
        if (textLayoutHeight == 0 || subTextLayoutHeight == 0 || Spacing == null)
        {
            spacing = 0.0;
        }
        else
        {
            spacing = Spacing.Value;
        }
        
        // max szélesség a kettő között
        var width = Math.Max(textLayoutWidth, subTextLayoutWidth);
        
        // végső méret a szélességgel és a max magassággal inflatelve a paddinggel
        var size = LayoutHelper.RoundLayoutSizeUp(new Size(width, textLayoutHeight + subTextLayoutHeight + spacing).Inflate(padding), 1, 1);

        return size;
    }

    // pozicionálja az elemeket a számukra elérhető hely alapján több metódussal együtt dolgozva (Arrange, ArrangeCore)
    // több infó: https://docs.avaloniaui.net/docs/basics/user-interface/building-layouts/#measuring-and-arranging-children
    protected override Size ArrangeOverride(Size finalSize)
    {
        var scale = LayoutHelper.GetLayoutScale(this);
        var padding = LayoutHelper.RoundLayoutThickness(Padding ?? new Thickness(0,0,0,0), scale, scale);
        var availableSize = finalSize.Deflate(padding);
        
        _textLayout?.Dispose();
        _textLayout = null;
        _subTextLayout?.Dispose();
        _subTextLayout = null;
        _constraint = availableSize;
        
        _textLayout = CreateTextLayout();
        _subTextLayout = CreateSubTextLayout();

        return finalSize;
    }
    
    // Kiszámítja az alap szöveg TextLayoutjának a pozícióját egy megadott igazítás szerint
    private Point CalculateTextPosition(TextAlignment? alignment)
    {
        var scale = LayoutHelper.GetLayoutScale(this);
        var padding = LayoutHelper.RoundLayoutThickness(Padding ?? new Thickness(0,0,0,0), scale, scale);

        // alapértelmezett balra igazítás, a kezdő pozíciót a paddingtől adjuk meg, hogy a padding benne legyen
        var x = padding.Left;
        var y = padding.Top;
        
        var subTextLayoutWidth = SubTextLayout?.Width ?? 0;

        switch (alignment)
        {
            // ha középre igazítás van akkor vesszük a Control szélességét és a textlayoutok közül a legnagyobbat
            // ez úgy igazítja a szöveget hogy a kezdőpozíciója bal oldalról haladva ott legyen hogy pont középre álljon
            // pl. ha 200 a control szélesség és 100 a leghosszabb textlayout akkor 50 lesz a kezdőpozíció
            // és mivel a textLayout legnagyobb szélessége még 100-at megy így ugyanúgy 50 fog kimaradni a jobb oldalt is
            case Avalonia.Media.TextAlignment.Center:
                x = (Bounds.Width - Math.Max(subTextLayoutWidth, TextLayout!.Width)) / 2;
                break;
            // vesszük a Control szélességet amiből szintén kivonjuk a leghosszabb textlayout szélességet és a jobb paddinget is
            // ugyanúgy ha 200 a bounds és 100 a textlayout maxwidth akkor abból a 100-ból még kivonjuk a paddinget ami
            // mondjuk 20, így a kezdőpozíció ebben az esetben 80 lenne
            // tehát a textlayout kezdene 80-ről bal oldalt, megy 100-at és marad 20 a paddingnek, így jobbra lesz igazítva
            case Avalonia.Media.TextAlignment.Right or Avalonia.Media.TextAlignment.End:
                x = Bounds.Width - Math.Max(subTextLayoutWidth, TextLayout!.Width) - padding.Right;
                break;
        }

        var calculatedPosition = new Point(x, y);
        _textLayoutPosition = calculatedPosition;

        return calculatedPosition;
    }
    
    // hasonlóan az alap szöveghez itt is kiszámolja a pozíciót, de a spacinget is figyelembe veszi
    private Point CalculateSubTextPosition(TextAlignment? alignment)
    {
        var scale = LayoutHelper.GetLayoutScale(this);
        var padding = LayoutHelper.RoundLayoutThickness(Padding ?? new Thickness(0,0,0,0), scale, scale);
        double spacing;
        if (TextLayout == null || Spacing == null)
        {
            spacing = 0.0;
        }
        else
        {
            spacing = Spacing.Value;
        }

        var textLayoutWidth = TextLayout?.Width ?? 0;
        var textLayoutHeight = TextLayout?.Height ?? 0;

        // alapértelmezett balra igazítás
        var x = padding.Left;
        var y = padding.Top + textLayoutHeight + spacing; // spacing hozzáadása ha van

        switch (alignment)
        {
            case Avalonia.Media.TextAlignment.Center:
                x = (Bounds.Width - Math.Max(textLayoutWidth, SubTextLayout!.Width)) / 2;
                break;
            case Avalonia.Media.TextAlignment.Right or Avalonia.Media.TextAlignment.End:
                x = Bounds.Width - Math.Max(textLayoutWidth, SubTextLayout!.Width) - padding.Right;
                break;
        }

        return new Point(x, y);
    }
    
    // ha bármelyik property megváltozik akkor invalidáljuk a jelenlegi textlayoutokat és újat hozunk létre
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        switch (change.Property.Name)
        {
            case nameof(Text):
            case nameof(SubText):
            case nameof(TextColor):
            case nameof(SubTextColor):
            case nameof(Background):
            case nameof(TextFontSize):
            case nameof(SubTextFontSize):
            case nameof(FontFamily):
            case nameof(Spacing):
            case nameof(TextAlignment):
            case nameof(SubTextAlignment):
            case nameof(Padding):
            case nameof(CornerRadius):
            case nameof(LineHeight):
            case nameof(SelectionColor):
            {
                InvalidateTextLayouts();
                break;
            }
        }
    }

    // IBeam kurzor kiválasztása szöveg felett (no genAI)
    // TODO: szöveg kijelölése, kimásolása, háttér kijelölés esetén
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        if (TextLayout == null || _textLayoutPosition == null) return;
        
        var pointerPosition = e.GetPosition(this);
        
        var textFromX = _textLayoutPosition.Value.X;
        var textToX = (_textLayoutPosition.Value.X + TextLayout.Width);
        
        var textFromY = _textLayoutPosition.Value.Y;
        var textToY = _textLayoutPosition.Value.Y + TextLayout.Height;
        
        // ha benne van a szövegdobozban
        if (pointerPosition.X >= textFromX && pointerPosition.X <= textToX
            && pointerPosition.Y >= textFromY && pointerPosition.Y <= textToY)
        {
            // pointer pozíciója a szövegdobozon belül, lekerekítve hogy ne legyenek kisebb eltérések double miatt
            var pointerPosYInBox = Math.Round(pointerPosition.Y, 2) - Math.Round(textFromY, 2);
            var pointerPosXInBox = Math.Round(pointerPosition.X, 2) - Math.Round(textFromX, 2);
        
            var textLineHeight = Math.Round(TextLayout.Height / TextLayout.TextLines.Count, 2);
            
            // hanyadik szövegsorban van a kurzor
            // lefele kerekítés intre konverzióval, hogy az a sor legyen kiválasztva amihez a legközelebb van a kurzor
            var linePointerPosY = Math.Round(pointerPosYInBox/textLineHeight, 2);
            var linePointerIndex = (int)linePointerPosY;
            
            // kivonjuk az adott sor magasságából a pixelpontos magasságot
            // de leosztjuk kettővel mert a pixelpontos magasság középen lesz, és külön kezeljük a felső és az alsó részt
            var heightDifference = (textLineHeight - TextLayout.TextLines[linePointerIndex].Extent) / 2;
            
            // a kiválasztott sor kezdési és végződési pozíciója függőlegesen
            var lineStartingPosY = textLineHeight * linePointerIndex;
            var lineEndingPosY = textLineHeight * (linePointerIndex + 1);
            
            // a sorban lévő extent kezdési és végződési pozíciója függőlegesen
            // itt figyeljük majd, hogy ebben benne van-e a cursor pointer, és ha igen akkor az szöveg
            var extentStartingPosY = lineStartingPosY + heightDifference;
            var extentEndingPosY = lineEndingPosY - heightDifference;
            
            // ha vízszintesen és függőlegesen nincs a kurzor a szövegen
            // vízszintesen ellenőrzi úgy hogy veszi az adott sor legnagyobb szélességét és ha az alatti akkor nincs ott
            // az alignmentet nem kell figyelni mert a szövegdoboz kerete az alignmenthez már igazodott
            // függőlegesen pedig veszi a pixelpontos magasságot és a sormagasságot, és ha az extenten kívül van akkor
            // nincs a szövegen
            if (TextLayout.TextLines[linePointerIndex].Width < pointerPosXInBox 
                || (pointerPosYInBox < extentStartingPosY || pointerPosYInBox > extentEndingPosY))
            {
                Cursor = new Cursor(StandardCursorType.Arrow);
                return;
            } 
            Cursor = new Cursor(StandardCursorType.Ibeam);
            return;
        }
        Cursor = new Cursor(StandardCursorType.Arrow);
    }
    
    
}