// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Threading.Tasks;
using System.Windows.Input;
using avallama.Constants;
using avallama.Utilities;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Media.TextFormatting;

namespace avallama.Controls;

/* TODO:
- md-s formázások
*/

/// <summary>
/// Szövegdoboz állítható háttérrel, két szöveggel, személyre szabott propertykkel
/// </summary>
public class GeneralBlock : Control
{
    // AXAML Styled Propertyk
    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<GeneralBlock, string?>(nameof(Text));

    public static readonly StyledProperty<IBrush?> TextColorProperty =
        AvaloniaProperty.Register<GeneralBlock, IBrush?>(nameof(TextColor));

    public static readonly StyledProperty<string?> SubTextProperty =
        AvaloniaProperty.Register<GeneralBlock, string?>(nameof(SubText));

    public static readonly StyledProperty<IBrush?> SubTextColorProperty =
        AvaloniaProperty.Register<GeneralBlock, IBrush?>(nameof(SubTextColor));

    public static readonly StyledProperty<Thickness?> PaddingProperty =
        AvaloniaProperty.Register<GeneralBlock, Thickness?>(nameof(Padding));

    public static readonly StyledProperty<CornerRadius?> CornerRadiusProperty =
        AvaloniaProperty.Register<GeneralBlock, CornerRadius?>(nameof(CornerRadius));

    public static readonly StyledProperty<double?> TextFontSizeProperty =
        AvaloniaProperty.Register<GeneralBlock, double?>(nameof(TextFontSize));

    public static readonly StyledProperty<double?> SubTextFontSizeProperty =
        AvaloniaProperty.Register<GeneralBlock, double?>(nameof(SubTextFontSize));

    public static readonly StyledProperty<TextAlignment?> TextAlignmentProperty =
        AvaloniaProperty.Register<GeneralBlock, TextAlignment?>(nameof(TextAlignment));

    public static readonly StyledProperty<TextAlignment?> SubTextAlignmentProperty =
        AvaloniaProperty.Register<GeneralBlock, TextAlignment?>(nameof(SubTextAlignment));

    public static readonly StyledProperty<FontFamily?> FontFamilyProperty =
        AvaloniaProperty.Register<GeneralBlock, FontFamily?>(nameof(FontFamily));

    public static readonly StyledProperty<IBrush?> BackgroundProperty =
        AvaloniaProperty.Register<GeneralBlock, IBrush?>(nameof(Background));

    public static readonly StyledProperty<double?> SpacingProperty =
        AvaloniaProperty.Register<GeneralBlock, double?>(nameof(Spacing));

    public static readonly StyledProperty<double?> LineHeightProperty =
        AvaloniaProperty.Register<GeneralBlock, double?>(nameof(LineHeight));

    public static readonly StyledProperty<IBrush?> SelectionColorProperty =
        AvaloniaProperty.Register<GeneralBlock, IBrush?>(nameof(SelectionColor));

    public static readonly StyledProperty<bool> SelectableProperty =
        AvaloniaProperty.Register<GeneralBlock, bool>(nameof(Selectable));

    public static readonly StyledProperty<bool> ClickableProperty =
        AvaloniaProperty.Register<GeneralBlock, bool>(nameof(Clickable));

    public static readonly StyledProperty<ICommand?> CommandProperty =
        AvaloniaProperty.Register<GeneralBlock, ICommand?>(nameof(Command));

    // saját ID
    public static readonly DirectProperty<GeneralBlock, Guid?> IdProperty =
        AvaloniaProperty.RegisterDirect<GeneralBlock, Guid?>(
            nameof(Id),
            o => o.Id,
            (o, v) => o.Id = v,
            unsetValue: Guid.Empty
        );

    // a jelenleg kiválasztott ID, ez összehasonlításhoz kell, hogy más stílust lehessen beállítani ha a kiválasztott control a jelenlegi
    public static readonly DirectProperty<GeneralBlock, Guid?> SelectedIdProperty =
        AvaloniaProperty.RegisterDirect<GeneralBlock, Guid?>(
            nameof(SelectedId),
            o => o.SelectedId,
            (o, v) => o.SelectedId = v,
            unsetValue: Guid.Empty
        );

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

    public ICommand? Command
    {
        get => GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public bool Selectable
    {
        get => GetValue(SelectableProperty);
        set => SetValue(SelectableProperty, value);
    }

    public bool Clickable
    {
        get => GetValue(ClickableProperty);
        set => SetValue(ClickableProperty, value);
    }

    private Guid? _id;

    public Guid? Id
    {
        get => _id;
        set => SetAndRaise(IdProperty, ref _id, value);
    }

    private Guid? _selectedId;

    public Guid? SelectedId
    {
        get => _selectedId;
        set => SetAndRaise(SelectedIdProperty, ref _selectedId, value);
    }

    private TextLayout? _textLayout;
    private TextLayout? _subTextLayout;

    // a MaxWidth és MaxHeight megfelelő beállításához kell a Create(Sub)TextLayoutnak
    private Size _constraint = Size.Infinity;

    private Point? _textLayoutPosition;

    private int _selectionStart;
    private int _selectionEnd;
    private string _selectedText = string.Empty;

    public GeneralBlock()
    {
        // focusable mert azt akarjuk hogy el lehessen kapni benne a fókuszt és el is lehessen veszíteni
        Focusable = true;

        // a tunnel routingstrategies miatt tudja megkapni a keydowneventeket előbb a messageblock
        AddHandler(KeyDownEvent, OnKeyDownHandler, RoutingStrategies.Tunnel);
    }

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
        IBrush? background;
        if (Id != null && SelectedId != null && Id == SelectedId)
        {
            background = ColorProvider.GetColor(AppColor.PrimaryContainer);
        }
        else
        {
            background = Background;
        }
        
        if (background == null) return;
        var cornerRadius = CornerRadius ?? new CornerRadius(0, 0, 0, 0);
        context.DrawRectangle(background, null,
            new RoundedRect(
                new Rect(Bounds.Size),
                cornerRadius.TopLeft,
                cornerRadius.TopRight,
                cornerRadius.BottomRight,
                cornerRadius.BottomLeft
            )
        );
    }

    // Létrehozott TextLayoutok renderelése (amennyiben nem null)
    private void RenderText(DrawingContext context)
    {
        // megnézzük hogy van kijelölés, és ha igen annak megfelelően rendereljük először a hátterét
        // és ezt követően arra rárendereljük a módosított (kijelölt inverz színekkel rendelkező) szöveget

        if (_selectionStart != _selectionEnd && _textLayout != null && Selectable)
        {
            // mínusz értékek elkerülése miatt min, max
            var selectionFrom = Math.Min(_selectionStart, _selectionEnd);
            var selectionRange = Math.Max(_selectionStart, _selectionEnd) - selectionFrom;

            var rects = _textLayout.HitTestTextRange(selectionFrom, selectionRange);
            var selectedColor = (SelectionColor as ImmutableSolidColorBrush)?.Color ?? Colors.Teal;
            var selectionBrush = new ImmutableSolidColorBrush(
                selectedColor,
                0.5
            );
            var paddingLeft = Padding?.Left ?? 0;
            var paddingTop = Padding?.Top ?? 0;
            var origin = new Point(paddingLeft, paddingTop);
            using (context.PushTransform(Matrix.CreateTranslation(origin)))
            {
                foreach (var rect in rects)
                {
                    context.FillRectangle(selectionBrush, PixelRect.FromRect(rect, 1).ToRect(1));
                }
            }
        }

        _textLayout?.Draw(context, CalculateTextPosition(TextAlignment));
        _subTextLayout?.Draw(context, CalculateSubTextPosition(SubTextAlignment));
    }

    // Létrehozza az alap szöveget (amennyiben meg van adva)
    private TextLayout? CreateTextLayout()
    {
        if (string.IsNullOrEmpty(Text)) return null;

        // typeface beállítása
        var typeface = new Typeface(
            FontFamily ?? FontFamily.Default
        );

        IBrush? textColor;
        if (Id != null && SelectedId != null && Id == SelectedId)
        {
            textColor = ColorProvider.GetColor(AppColor.OnPrimaryContainer);
        }
        else
        {
            textColor = TextColor;
        }

        // real-time generálásnál képes több ezres nagyságban létrehozni és felszabadítani TextLayout elemeket
        return new TextLayout(
            Text,
            typeface,
            null,
            TextFontSize ?? 12,
            textColor,
            TextAlignment ?? Avalonia.Media.TextAlignment.Left,
            TextWrapping.Wrap,
            TextTrimming.None,
            null,
            FlowDirection.LeftToRight,
            _constraint.Width,
            _constraint.Height,
            LineHeight ?? double.NaN
        );
    }

    // Létrehozza az alsó szöveget (amennyiben meg van adva)
    private TextLayout? CreateSubTextLayout()
    {
        if (!string.IsNullOrEmpty(SubText))
        {
            IBrush? subTextColor;
            if (Id != null && SelectedId != null && Id == SelectedId)
            {
                subTextColor = ColorProvider.GetColor(AppColor.OnPrimaryContainer);
            }
            else
            {
                subTextColor = SubTextColor;
            }
            
            return new TextLayout(
                SubText,
                new Typeface(FontFamily ?? FontFamily.Default),
                SubTextFontSize ?? 8,
                subTextColor,
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

    private void InvalidateGeneralBlock()
    {
        _textLayout?.Dispose();
        _textLayout = null;
        _subTextLayout?.Dispose();
        _subTextLayout = null;
    }

    private void CreateGeneralBlock()
    {
        _textLayout = CreateTextLayout();
        _subTextLayout = CreateSubTextLayout();
    }

    protected override void OnMeasureInvalidated()
    {
        // felszabadítja a textLayoutokat
        InvalidateGeneralBlock();
        base.OnMeasureInvalidated();
    }

    // Felméri hogy mennyi helyre van szüksége a Controlnak
    protected override Size MeasureOverride(Size availableSize)
    {
        var scale = LayoutHelper.GetLayoutScale(this);

        // LayoutHelperrel roundolja a Thicknesst (megadott Paddingot) magas dpi képernyőkre, a megfelelő koordinátákhoz
        var padding = LayoutHelper.RoundLayoutThickness(Padding ?? new Thickness(0, 0, 0, 0), scale, scale);
        var deflatedSize = availableSize.Deflate(padding); // kiveszi az elérhető helyből a paddingot

        // ha a constraint nem egyezik akkor reseteli a textLayoutokat és újraigazítja őket
        if (_constraint != deflatedSize)
        {
            _textLayout?.Dispose();
            _textLayout = null;
            _subTextLayout?.Dispose();
            _subTextLayout = null;
            _constraint = deflatedSize;

            _textLayout = CreateTextLayout();
            _subTextLayout = CreateSubTextLayout();
        }

        // a lehető legnagyobb szélesség a textlayoutokra nézve
        var textLayoutWidth = _textLayout == null
            ? 0
            : _textLayout.OverhangLeading + _textLayout.WidthIncludingTrailingWhitespace + _textLayout.OverhangTrailing;
        var subTextLayoutWidth = _subTextLayout == null
            ? 0
            : _subTextLayout.OverhangLeading + _subTextLayout.WidthIncludingTrailingWhitespace +
              _subTextLayout.OverhangTrailing;

        // a lehető legnagyobb hosszúság a textlayoutokra nézve
        var textLayoutHeight = _textLayout?.Height ?? 0;
        var subTextLayoutHeight = _subTextLayout?.Height ?? 0;

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
        var size = LayoutHelper.RoundLayoutSizeUp(
            new Size(width, textLayoutHeight + subTextLayoutHeight + spacing).Inflate(padding), 1, 1);

        return size;
    }

    // pozicionálja az elemeket a számukra elérhető hely alapján több metódussal együtt dolgozva (Arrange, ArrangeCore)
    // több infó: https://docs.avaloniaui.net/docs/basics/user-interface/building-layouts/#measuring-and-arranging-children
    protected override Size ArrangeOverride(Size finalSize)
    {
        var scale = LayoutHelper.GetLayoutScale(this);
        var padding = LayoutHelper.RoundLayoutThickness(Padding ?? new Thickness(0, 0, 0, 0), scale, scale);
        var availableSize = finalSize.Deflate(padding);

        if (_constraint != availableSize)
        {
            _textLayout?.Dispose();
            _textLayout = null;
            _subTextLayout?.Dispose();
            _subTextLayout = null;
            _constraint = availableSize;

            _textLayout = CreateTextLayout();
            _subTextLayout = CreateSubTextLayout();
        }

        return finalSize;
    }

    // Kiszámítja az alap szöveg TextLayoutjának a pozícióját egy megadott igazítás szerint
    private Point CalculateTextPosition(TextAlignment? alignment)
    {
        var scale = LayoutHelper.GetLayoutScale(this);
        var padding = LayoutHelper.RoundLayoutThickness(Padding ?? new Thickness(0, 0, 0, 0), scale, scale);

        // alapértelmezett balra igazítás, a kezdő pozíciót a paddingtől adjuk meg, hogy a padding benne legyen
        var x = padding.Left;
        var y = padding.Top;

        var subTextLayoutWidth = _subTextLayout?.Width ?? 0;

        switch (alignment)
        {
            // ha középre igazítás van akkor vesszük a Control szélességét és a textlayoutok közül a legnagyobbat
            // ez úgy igazítja a szöveget hogy a kezdőpozíciója bal oldalról haladva ott legyen hogy pont középre álljon
            // pl. ha 200 a control szélesség és 100 a leghosszabb textlayout akkor 50 lesz a kezdőpozíció
            // és mivel a textLayout legnagyobb szélessége még 100-at megy így ugyanúgy 50 fog kimaradni a jobb oldalt is
            case Avalonia.Media.TextAlignment.Center:
                x = (Bounds.Width - Math.Max(subTextLayoutWidth, _textLayout!.Width)) / 2;
                break;
            // vesszük a Control szélességet amiből szintén kivonjuk a leghosszabb textlayout szélességet és a jobb paddinget is
            // ugyanúgy ha 200 a bounds és 100 a textlayout maxwidth akkor abból a 100-ból még kivonjuk a paddinget ami
            // mondjuk 20, így a kezdőpozíció ebben az esetben 80 lenne
            // tehát a textlayout kezdene 80-ről bal oldalt, megy 100-at és marad 20 a paddingnek, így jobbra lesz igazítva
            case Avalonia.Media.TextAlignment.Right or Avalonia.Media.TextAlignment.End:
                x = Bounds.Width - Math.Max(subTextLayoutWidth, _textLayout!.Width) - padding.Right;
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
        var padding = LayoutHelper.RoundLayoutThickness(Padding ?? new Thickness(0, 0, 0, 0), scale, scale);
        double spacing;
        if (_textLayout == null || Spacing == null)
        {
            spacing = 0.0;
        }
        else
        {
            spacing = Spacing.Value;
        }

        var textLayoutWidth = _textLayout?.Width ?? 0;
        var textLayoutHeight = _textLayout?.Height ?? 0;

        // alapértelmezett balra igazítás
        var x = padding.Left;
        var y = padding.Top + textLayoutHeight + spacing; // spacing hozzáadása ha van

        switch (alignment)
        {
            case Avalonia.Media.TextAlignment.Center:
                x = (Bounds.Width - Math.Max(textLayoutWidth, _subTextLayout!.Width)) / 2;
                break;
            case Avalonia.Media.TextAlignment.Right or Avalonia.Media.TextAlignment.End:
                x = Bounds.Width - Math.Max(textLayoutWidth, _subTextLayout!.Width) - padding.Right;
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
            // Méretet érintő változások:
            case nameof(Text):
            case nameof(SubText):
            case nameof(TextFontSize):
            case nameof(SubTextFontSize):
            case nameof(FontFamily):
            case nameof(Spacing):
            case nameof(Padding):
            case nameof(LineHeight):
            {
                InvalidateMeasure();
                break;
            }

            // Vizuális változások:
            case nameof(TextColor):
            case nameof(SubTextColor):
            case nameof(Background):
            case nameof(SelectionColor):
            case nameof(CornerRadius):
            case nameof(Id):
            case nameof(SelectedId):
            {
                InvalidateGeneralBlock();
                CreateGeneralBlock();
                InvalidateVisual();
                break;
            }

            case nameof(TextAlignment):
            case nameof(SubTextAlignment):
            {
                InvalidateArrange();
                break;
            }
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        if (_textLayout == null || Text == null) return;

        if (Selectable)
        {
            var pointerPosition = e.GetPosition(this);

            var isPointerOverText = IsPointerOverText(pointerPosition);
            if (isPointerOverText)
            {
                Cursor = new Cursor(StandardCursorType.Ibeam);
                if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
                {
                    var textIndex = TextIndexFromPointer(e.GetPosition(this));
                    _selectionEnd = textIndex;
                    InvalidateVisual();
                }
            }
            else
            {
                Cursor = new Cursor(StandardCursorType.Arrow);
            }
        }
        else if (Clickable)
        {
            Cursor = new Cursor(StandardCursorType.Hand);
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        if (_textLayout == null || Text == null) return;

        if (Selectable)
        {
            var textIndex = TextIndexFromPointer(e.GetPosition(this));
            _selectionStart = textIndex;

            switch (e.ClickCount)
            {
                case 1:
                    if (_selectedText.Length > 0)
                    {
                        ClearSelection();
                    }

                    break;
                case 2:
                    SelectWordByIndex(textIndex);
                    break;
                case >= 3:
                    SelectParagraphByIndex(textIndex);
                    break;
            }
        }
        else if (Clickable)
        {
            if (Id is null) return;
            if (Command is not null && Command.CanExecute(Id))
            {
                Command.Execute(Id);
            }
        }
    }

    // kattintott szó kiválasztása index alapján (pl. dupla klikkre)
    private void SelectWordByIndex(int index)
    {
        if (_textLayout == null || Text == null || index < 0 || index >= Text.Length || !Selectable) return;
        if (char.IsWhiteSpace(Text[index]) || !char.IsLetterOrDigit(Text[index])) return;
        var wordStartIndex = 0;
        var wordEndIndex = 0;
        var i = index;

        // balra haladva megnézzük hogy hol kezdődik az adott szó
        for (; i != -1 && !char.IsWhiteSpace(Text[i]) && char.IsLetterOrDigit(Text[i]); i--)
        {
            wordStartIndex = i;
        }

        i = index;

        // jobbra haladva megnézzük hogy hol végződik az adott szó
        for (; i != Text.Length && !char.IsWhiteSpace(Text[i]) && char.IsLetterOrDigit(Text[i]); i++)
        {
            wordEndIndex = i;
        }

        _selectionStart = wordStartIndex;
        _selectionEnd = wordEndIndex + 1;
        InvalidateVisual();
        UpdateSelectedText();
    }

    private void SelectParagraphByIndex(int index)
    {
        if (_textLayout == null || Text == null || !Selectable) return;
        const string separator = "\n";
        int paragraphStartIndex, paragraphEndIndex;
        var firstSeparatorPosition = Text.LastIndexOf(separator, index, StringComparison.Ordinal);
        if (firstSeparatorPosition == -1)
        {
            paragraphStartIndex = 0;
        }
        else
        {
            paragraphStartIndex = firstSeparatorPosition + separator.Length;
        }

        var lastSeparatorPosition = Text.IndexOf(separator, index, StringComparison.Ordinal);
        if (lastSeparatorPosition == -1)
        {
            paragraphEndIndex = Text.Length - 1;
        }
        else
        {
            paragraphEndIndex = lastSeparatorPosition;
        }

        _selectionStart = paragraphStartIndex;
        _selectionEnd = paragraphEndIndex + 1;
        InvalidateVisual();
        UpdateSelectedText();
    }

    private void SelectAllText()
    {
        if (_textLayout == null || Text == null || !Selectable) return;
        _selectionStart = 0;
        _selectionEnd = Text.Length;
        InvalidateVisual();
    }

    private void ClearSelection()
    {
        if (!Selectable) return;
        _selectionEnd = _selectionStart;
        _selectedText = string.Empty;
        InvalidateVisual();
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        if (!Selectable) return;
        // a kijelölés végén mentjük el hogy ne kelljen folyamatosan frissíteni a stringet
        UpdateSelectedText();
    }

    private void UpdateSelectedText()
    {
        if (_textLayout == null || Text == null || !Selectable) return;
        var selectionFrom = Math.Min(_selectionStart, _selectionEnd);
        var selectionRange = Math.Max(_selectionStart, _selectionEnd) - selectionFrom;
        _selectedText = Text.Substring(selectionFrom, selectionRange);
    }

    // kijelölés helyének meghatározása a SelectableTextBlockhoz hasonlóan
    // a paddingokat bele kell venni ahhoz hogy visszaadja a megfelelő textPositiont
    private int TextIndexFromPointer(Point pointerPosition)
    {
        if (_textLayout == null || Text == null || !Selectable) return -1;
        var padding = Padding ?? new Thickness(0, 0, 0, 0);
        var point = pointerPosition - new Point(padding.Left, padding.Top);

        point = new Point(
            Math.Clamp(point.X, 0, Math.Max(_textLayout.WidthIncludingTrailingWhitespace, 0)),
            Math.Clamp(point.Y, 0, Math.Max(_textLayout.Height, 0))
        );

        var hit = _textLayout.HitTestPoint(point);
        // azért Text.Length és nem Text.Length - 1, mert akkor nem lehet az utolsó elemből kiindulva kijelölni
        return Math.Clamp(hit.TextPosition, 0, Text.Length);
    }

    /// <summary>
    /// Ellenőrzi, hogy rajta van-e a cursor pointer a szövegen
    /// </summary>
    /// <returns>
    /// Egy <see cref="bool"/> érték arra vonatkozóan hogy a pointer a szövegen van-e
    /// </returns>
    private bool IsPointerOverText(Point pointerPosition)
    {
        if (_textLayout == null || _textLayoutPosition == null || !Selectable) return false;
        var textFromX = _textLayoutPosition.Value.X;
        var textToX = (_textLayoutPosition.Value.X + _textLayout.Width);

        var textFromY = _textLayoutPosition.Value.Y;
        var textToY = _textLayoutPosition.Value.Y + _textLayout.Height;

        // ha nincs benne a pointer a szövegdobozban akkor visszatér false-al
        if (!(pointerPosition.X >= textFromX) || !(pointerPosition.X <= textToX)
                                              || !(pointerPosition.Y >= textFromY) ||
                                              !(pointerPosition.Y <= textToY)) return false;

        // pointer pozíciója a szövegdobozon belül, lekerekítve hogy ne legyenek kisebb eltérések double miatt
        var pointerPosYInBox = Math.Round(pointerPosition.Y, 2) - Math.Round(textFromY, 2);
        var pointerPosXInBox = Math.Round(pointerPosition.X, 2) - Math.Round(textFromX, 2);

        var textLineHeight = Math.Round(_textLayout.Height / _textLayout.TextLines.Count, 2);

        // hanyadik szövegsorban van a kurzor
        // lefele kerekítés intre konverzióval, hogy az a sor legyen kiválasztva amihez a legközelebb van a kurzor
        var linePointerPosY = Math.Round(pointerPosYInBox / textLineHeight, 2);
        var linePointerIndex = Math.Clamp((int)linePointerPosY, 0, _textLayout.TextLines.Count - 1);

        // az adott szöveg sorának az indexét elmentjük, amin a kurzor van
        // _pointedLineIndex = linePointerIndex;

        // kivonjuk az adott sor magasságából a pixelpontos magasságot
        // de leosztjuk kettővel mert a pixelpontos magasság középen lesz, és külön kezeljük a felső és az alsó részt
        var heightDifference = (textLineHeight - _textLayout.TextLines[linePointerIndex].Extent) / 2;

        // a kiválasztott sor kezdési és végződési pozíciója függőlegesen
        var lineStartingPosY = textLineHeight * linePointerIndex;
        var lineEndingPosY = textLineHeight * (linePointerIndex + 1);

        // a sorban lévő extent kezdési és végződési pozíciója függőlegesen
        // itt figyeljük majd, hogy ebben benne van-e a cursor pointer, és ha igen akkor az szöveg
        var extentStartingPosY = lineStartingPosY + heightDifference;
        var extentEndingPosY = lineEndingPosY - heightDifference;

        // vízszintesen ellenőrzi úgy hogy veszi az adott sor legnagyobb szélességét és ha az alatti akkor nincs ott
        // az alignmentet nem kell figyelni mert a szövegdoboz kerete az alignmenthez már igazodott
        // függőlegesen pedig veszi a pixelpontos magasságot és a sormagasságot, és ha az extenten kívül van akkor nincs a szövegen
        return !(_textLayout.TextLines[linePointerIndex].Width < pointerPosXInBox)
               && !(pointerPosYInBox < extentStartingPosY) && !(pointerPosYInBox > extentEndingPosY);
    }

    protected override void OnGotFocus(GotFocusEventArgs e)
    {
        base.OnGotFocus(e);
        if (!Selectable) return;
        UpdateSelectedText();
    }

    protected override void OnLostFocus(RoutedEventArgs e)
    {
        base.OnLostFocus(e);
        if (!Selectable) return;
        if (ContextFlyout is not { IsOpen: true } &&
            ContextMenu is not { IsOpen: true })
        {
            ClearSelection();
        }

        UpdateSelectedText();
    }

    // nesze neked async
    private void OnKeyDownHandler(object? sender, KeyEventArgs e)
    {
        _ = OnKeyDown(sender, e);
    }

    private async Task OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (!Selectable) return;

        // macOS billentyűk
        // Meta - nyomva tartott Command
        // LWin - lenyomott bal oldali Command
        // RWin - lenyomott jobb oldali Command

        // CTRL+A - összes szöveg kijelölése
        if (e.Key == Key.A && (e.KeyModifiers.HasFlag(KeyModifiers.Control)
                               || e.KeyModifiers.HasFlag(KeyModifiers.Meta)))
        {
            SelectAllText();
            e.Handled = true;
        }

        // CTRL+C - szöveg kimásolása vágólapra
        if (e.Key == Key.C && (e.KeyModifiers.HasFlag(KeyModifiers.Control)
                               || e.KeyModifiers.HasFlag(KeyModifiers.Meta)))
        {
            UpdateSelectedText();
            await CopyToClipboardAsync(_selectedText);
        }
    }

    private async Task CopyToClipboardAsync(string textToCopy)
    {
        if (VisualRoot is TopLevel topLevel)
        {
            var clipboard = topLevel.Clipboard;
            if (clipboard == null) return;
            await clipboard.SetTextAsync(textToCopy);
        }
    }
}