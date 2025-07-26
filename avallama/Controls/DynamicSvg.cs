// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Media;

namespace avallama.Controls;

/// <summary>
/// Svg megjelenítésére használható. A FillColor és a StrokeColor propertykkel megadható az SVG színe.
/// </summary>

public class DynamicSvg(IServiceProvider provider) : Avalonia.Svg.Svg(provider)
{
    /* SVG-t a következőképp lehet színezni megfelelően ezzel az osztállyal:
     * - Az SVG fájl tartalmát módosítani kell úgy, hogy azok a részek, amiket színezni szeretnénk külön fill és stroke attribútumban legyenek.
     * - Ez azt jelenti hogy ha pl. style="fill:none;stroke:none;" formátumban van akkor le kell cserélni úgy hogy style helyett
     * fill="none" stroke="none"
     * - Ha pedig valamelyik elemet ki akarjuk hagyni a színezésből vagy statikusan meg akarunk adni neki valamit akkor fordítva,
     * tehát style-ba szervezzük úgy hogy a DynamicSvg ne használja
     */
    
    // 'fill-opacity' property hozzáadása esetleg később
     
    public static readonly StyledProperty<IBrush?> FillColorProperty = 
        AvaloniaProperty.Register<DynamicSvg, IBrush?>("FillColor");
    
    public static readonly StyledProperty<IBrush?> StrokeColorProperty = 
        AvaloniaProperty.Register<DynamicSvg, IBrush?>("StrokeColor");

    public IBrush? FillColor
    {
        get => GetValue(FillColorProperty);
        set => SetValue(FillColorProperty, value);
    }

    public IBrush? StrokeColor
    {
        get => GetValue(StrokeColorProperty);
        set => SetValue(StrokeColorProperty, value);
    }
    
    // public DynamicSvg(Uri baseUri) : base(baseUri) { }

    private static string ConvertColorToHex(IBrush? propertyColor)
    {
        var colorParse = Color.TryParse(propertyColor?.ToString(), out var color);
        if (!colorParse) return "000000"; // fekete szín ha a parse sikertelen
        var rgb = color.ToUInt32(); 
        var result = $"{rgb.ToString("x8", CultureInfo.InvariantCulture)}"; // hex-re parsolja
        return result[2..]; // az első 2 karaktert leviszi, különben nem állítható be a szín

    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == FillColorProperty)
        {
            SetCurrentValue(CssProperty, $"* {{ fill: #{ConvertColorToHex(FillColor)} }}; ");
        }

        if (change.Property == StrokeColorProperty)
        {
            SetCurrentValue(CssProperty, $"* {{ stroke: #{ConvertColorToHex(StrokeColor)} }}; ");
        }
    }
}