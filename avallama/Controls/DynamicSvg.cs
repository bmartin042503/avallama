using System;
using System.Globalization;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace avallama.Controls;

/// <summary>
/// Svg megjelenítésére használható. A FillColor-al megadható az SVG színe.
/// (Komplexebb objektumok esetén, illetve beágyazott színbeállításoknál nem működik jelenleg)
/// </summary>

public class DynamicSvg : Avalonia.Svg.Svg
{
    public static readonly StyledProperty<IBrush?> FillColorProperty = 
        AvaloniaProperty.Register<DynamicSvg, IBrush?>("FillColor");

    public IBrush? FillColor
    {
        get => GetValue(FillColorProperty);
        set => SetValue(FillColorProperty, value);
    }
    
    // public DynamicSvg(Uri baseUri) : base(baseUri) { }
    public DynamicSvg(IServiceProvider provider) : base(provider) { }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == FillColorProperty)
        {
            var color = Colors.Transparent; // ebbe próbálja parsolni a megadott colort
            var colorParse = Color.TryParse(FillColor?.ToString(), out color);
            var rgb = color.ToUInt32(); 
            var result = $"{rgb.ToString("x8", CultureInfo.InvariantCulture)}"; // hex-re parsolja
            result = result.Substring(2); // az első 2 karaktert leviszi, különben nem állítható be a szín
            if (colorParse)
            {
                // az svg fájlban az összes elemre beállítja az adott színt
                // ha komplexebb, esetleg elemen belül beágyazottan van megadva az svgben a szín akkor nem írható felül jelenleg
                SetCurrentValue(CssProperty, $"* {{ fill: #{result}; stroke: #{result} ");
            }
        }
    }
}