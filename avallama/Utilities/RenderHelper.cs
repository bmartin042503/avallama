// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Globalization;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Platform;
using Avalonia.Svg;
using Svg.Model;

namespace avallama.Utilities;

public static class RenderHelper
{
    public static readonly FontFamily ManropeFont = new("avares://avallama/Assets/Fonts/#Manrope");
    
    public static string? BrushToHex(IBrush? brush)
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
    
    // svg betöltése megadott szín és áttetszőség alapján
    // renderelésre készíti elő, ahhoz hogy jól megjelenjen még kell eltolásokat, skálázást adni 'Matrix' típusban
    public static AvaloniaPicture? LoadSvg(
        string svgPath,
        IBrush? svgColor = null,
        double svgOpacity = 0.0
    )
    {
        // szín és opacity beállítása css-el, úgy hogy a megadott színt átalakítjuk hex-re hogy css-nek át tudjuk adni
        // opacity-nél invariantculture kell, különben máshogy nem működik, vagyis vesszővel nem jó, pont kell a tizedesjegy elé
        var css =
            $"* {{ fill: {BrushToHex(ColorProvider.GetColor(AppColor.OnSurface))}; fill-opacity: {svgOpacity.ToString("0.0", CultureInfo.InvariantCulture)}; }}";
        var parameters = new SvgParameters(null, css);

        // svg betöltése
        using var stream = AssetLoader.Open(new Uri($"avares://avallama/{svgPath}"));
        var svgPicture = SvgSource.LoadPicture(stream, parameters);
        return svgPicture is null ? null : AvaloniaPicture.Record(svgPicture);
    }
}