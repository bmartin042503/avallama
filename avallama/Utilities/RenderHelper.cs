// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Globalization;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Platform;
using Avalonia.Svg;
using ShimSkiaSharp;
using Svg.Model;

namespace avallama.Utilities;

public static class RenderHelper
{
    public static readonly FontFamily ManropeFont = new("avares://avallama/Assets/Fonts/#Manrope");
    
    public const string DownloadSvgPath = "Assets/Svg/download.svg";
    public const string SpinnerSvgPath = "Assets/Svg/spinner.svg";
    public const string PauseSvgPath = "Assets/Svg/pause.svg";
    
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

    private static int _allocationCount = 0;
    
    // svg betöltése megadott szín és áttetszőség alapján
    // renderelésre készíti elő, a render pozícióját Matrix eltolásokban lehet megadni, ezt a drawingcontext-re kell alkalmazni (context.PushTransform())
    // ahhoz hogy rajzolható legyen meg kell hívni ezt: AvaloniaPicture.Record(svgPicture) ez visszaad egy AvaloniaPicturet amin van Draw metódus
    // a színt úgy kell megadni ahogyan az az svg fájlban van (stroke vagy fill attól függően mit használ)
    public static SKPicture? LoadSvg(
        string path,
        IBrush? fillColor = null,
        IBrush? strokeColor = null,
        double opacity = 1.0
    )
    {
        // szín és opacity beállítása css-el, úgy hogy a megadott színt átalakítjuk hex-re hogy css-nek át tudjuk adni
        // opacity-nél invariantculture kell, különben máshogy nem működik, vagyis vesszővel nem jó, pont kell a tizedesjegy elé

        // css felépítése
        var css = "* { ";
        if (fillColor is not null)
        {
            css +=
                $"fill: {BrushToHex(fillColor)}; fill-opacity: {opacity.ToString("0.0", CultureInfo.InvariantCulture)}; ";
        }

        if (strokeColor is not null)
        {
            css +=
                $"stroke: {BrushToHex(strokeColor)}; stroke-opacity: {opacity.ToString("0.0", CultureInfo.InvariantCulture)}; ";
        }
        css += "}";
        
        var parameters = new SvgParameters(null, css);

        // svg betöltése
        using var stream = AssetLoader.Open(new Uri($"avares://avallama/{path}"));
        
        // TODO: optimalizálni, folyamatosan allokál
        _allocationCount++;
        // Console.WriteLine($"RenderHelper called and memory for SVG allocated : {_allocationCount}");
        var svgPicture = SvgSource.LoadPicture(stream, parameters);
        return svgPicture;
    }
}