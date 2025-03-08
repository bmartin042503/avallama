using System;
using System.Globalization;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace avallama.Controls;

/// <summary>
/// Svg megjelenítésére használható. A FillColor-al megadható az SVG színe.
/// </summary>

public class DynamicSvg : Avalonia.Svg.Svg
{
    /* SVG-t a következőképp lehet színezni megfelelően ezzel az osztállyal:
     * - Az SVG fájl tartalmát módosítani kell úgy, hogy azok a részek, amiket színezni szeretnénk külön fill és stroke attribútumban legyenek.
     * - Ez azt jelenti hogy ha pl. style="fill:none;stroke:none;" formátumban van akkor le kell cserélni úgy hogy style helyett
     * fill="none" stroke="none"
     * - Ha pedig valamelyik elemet ki akarjuk hagyni a színezésből vagy statikusan meg akarunk adni neki valamit akkor fordítva,
     * tehát style-ba szervezzük úgy hogy a DynamicSvg ne használja
     */
    
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
                SetCurrentValue(CssProperty, $"* {{ fill: #{result}; stroke: #{result} ");
            }
        }
    }
}