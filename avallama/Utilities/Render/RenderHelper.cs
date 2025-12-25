// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Globalization;
using avallama.Services;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Platform;
using Avalonia.Svg;
using ShimSkiaSharp;
using Svg.Model;

namespace avallama.Utilities.Render;

public static class RenderHelper
{
    public static readonly FontFamily ManropeFont = new("avares://avallama/Assets/Fonts/#Manrope");

    public const string DownloadSvgPath = "Assets/Svg/download.svg";
    public const string SpinnerSvgPath = "Assets/Svg/spinner.svg";
    public const string PauseSvgPath = "Assets/Svg/pause.svg";
    public const string CloseSvgPath = "Assets/Svg/close.svg";
    public const string ResumeSvgPath = "Assets/Svg/resume.svg";

    /* csak mert mindig elfelejtem, így kell opacity-s brusht megadni, pl. ahova IBrush kell:
        new SolidColorBrush(
            ColorProvider.GetColor(AppColor.OnSurface).Color,
            Opacity
        )
    */

    private static string? BrushToHex(IBrush? brush)
    {
        if (brush is not ImmutableSolidColorBrush solid) return null;
        var color = solid.Color;
        // 'X' means hexadecimal format
        // and 2 means always use two digits, pad with zero if necessary
        // if opaque, then #RRGGBB
        return color.A == 255
            ? $"#{color.R:X2}{color.G:X2}{color.B:X2}"
            :
            // ha van áttetszőség, akkor #AARRGGBB
            $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
    }


    // load svg with given color, opacity and stroke
    // it prepares it for rendering, the svg position can be given in Matrix translations, which should be applied to the drawingcontext (context.PushTransform())
    // to make it drawable, you have to call this: AvaloniaPicture.Record(svgPicture) which returns an AvaloniaPicture that has a Draw method
    // the color should be given as it is in the svg file (stroke or fill depending on what it uses)
    public static SKPicture? LoadSvg(
        string path,
        IBrush? fillColor = null,
        IBrush? strokeColor = null,
        double strokeWidth = double.NaN,
        double opacity = 1.0
    )
    {
        // color and opacity setting with css, converting the given color to hex so we can pass it to css
        // opacity needs invariantculture, otherwise it doesn't work properly, meaning comma is not good, dot is needed before the decimal

        // css construction
        var css = "* { ";
        if (fillColor is not null)
        {
            css +=
                $"fill: {BrushToHex(fillColor)}; fill-opacity: {opacity.ToString("0.0", CultureInfo.InvariantCulture)}; ";
        }

        if (!double.IsNaN(strokeWidth))
        {
            css +=
                $"stroke-width: {strokeWidth.ToString("0.0", CultureInfo.InvariantCulture)}; ";
        }

        if (strokeColor is not null)
        {
            css +=
                $"stroke: {BrushToHex(strokeColor)}; stroke-opacity: {opacity.ToString("0.0", CultureInfo.InvariantCulture)}; ";
        }
        css += "}";

        var parameters = new SvgParameters(null, css);

        // svg loading
        using var stream = AssetLoader.Open(new Uri($"avares://avallama/{path}"));

        var svgPicture = SvgSource.LoadPicture(stream, parameters);
        return svgPicture;
    }
}
