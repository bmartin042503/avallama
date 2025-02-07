using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;

namespace avallama.Controls;

/// <summary>
/// Szövegdoboz állítható háttérrel, két szöveggel, személyre szabott propertykkel
/// </summary>
public class MessageBlock : Control
{
    
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

    private TextLayout? _textLayout;
    private TextLayout? _subTextLayout;

    private Size _constraint = Size.Infinity;

    public TextLayout? TextLayout => _textLayout ??= CreateTextLayout();
    public TextLayout? SubTextLayout => _subTextLayout ??= CreateSubTextLayout();

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
    
    public override void Render(DrawingContext context)
    {
        RenderBackground(context);
        RenderText(context);
    }

    private void RenderBackground(DrawingContext context)
    {
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

    private void RenderText(DrawingContext context)
    {
        TextLayout?.Draw(context, CalculateTextPosition(TextAlignment));
        SubTextLayout?.Draw(context, CalculateSubTextPosition(SubTextAlignment));
    }

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
                _constraint.Height
            );
        }
        return null;
    }

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

    private void InvalidateTextLayouts()
    {
        InvalidateVisual();
        InvalidateMeasure();
    }

    protected override void OnMeasureInvalidated()
    {
        _textLayout?.Dispose();
        _textLayout = null;
        _subTextLayout?.Dispose();
        _subTextLayout = null;
        base.OnMeasureInvalidated();
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var scale = LayoutHelper.GetLayoutScale(this);
        var padding = LayoutHelper.RoundLayoutThickness(Padding ?? new Thickness(0,0,0,0), scale, scale);
        var deflatedSize = availableSize.Deflate(padding);

        if (_constraint != deflatedSize)
        {
            _textLayout?.Dispose();
            _textLayout = null;
            _subTextLayout?.Dispose();
            _subTextLayout = null;
            _constraint = deflatedSize;
            InvalidateArrange();
        }
        
        var textLayout = TextLayout;
        var subTextLayout = SubTextLayout;
        
        var textLayoutWidth = textLayout == null ? 0 : textLayout.OverhangLeading + textLayout.WidthIncludingTrailingWhitespace + textLayout.OverhangTrailing;
        var subTextLayoutWidth = subTextLayout == null ? 0 : subTextLayout.OverhangLeading + subTextLayout.WidthIncludingTrailingWhitespace + subTextLayout.OverhangTrailing;

        var textLayoutHeight = textLayout?.Height ?? 0;
        var subTextLayoutHeight = subTextLayout?.Height ?? 0;

        double spacing;
        if (textLayoutHeight == 0 || subTextLayoutHeight == 0 || Spacing == null)
        {
            spacing = 0.0;
        }
        else
        {
            spacing = Spacing.Value;
        }
        
        var width = Math.Max(textLayoutWidth, subTextLayoutWidth);
        var size = LayoutHelper.RoundLayoutSizeUp(new Size(width, textLayoutHeight + subTextLayoutHeight + spacing).Inflate(padding), 1, 1);

        return size;
    }

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

    private Point CalculateTextPosition(TextAlignment? alignment)
    {
        var scale = LayoutHelper.GetLayoutScale(this);
        var padding = LayoutHelper.RoundLayoutThickness(Padding ?? new Thickness(0,0,0,0), scale, scale);

        // alapértelmezett balra igazítás
        var x = padding.Left;
        var y = padding.Top;
        
        var subTextLayoutWidth = SubTextLayout?.Width ?? 0;

        switch (alignment)
        {
            case Avalonia.Media.TextAlignment.Center:
                x = (Bounds.Width - Math.Max(subTextLayoutWidth, TextLayout!.Width)) / 2;
                break;
            case Avalonia.Media.TextAlignment.Right or Avalonia.Media.TextAlignment.End:
                x = Bounds.Width - Math.Max(subTextLayoutWidth, TextLayout!.Width) - padding.Right;
                break;
        }

        return new Point(x, y);
    }
    
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
        var y = padding.Top + textLayoutHeight + spacing;

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
            {
                InvalidateTextLayouts();
                break;
            }
        }
    }
}