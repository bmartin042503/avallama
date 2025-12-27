// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using avallama.Constants;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Styling;

namespace avallama.Utilities.Render;

// returns the color for the given color key based on the current theme
public static class ColorProvider
{
    public static ImmutableSolidColorBrush GetColor(AppColor appColor)
    {
        var defaultColorBrush = new ImmutableSolidColorBrush(Colors.Black);

        object? color = defaultColorBrush;
        var themes = Application.Current?.Resources.ThemeDictionaries;
        if (themes is null) return defaultColorBrush;

        // gets the Light and Dark themes from the application's themes and returns the one in use
        themes.TryGetValue(
            Application.Current?.ActualThemeVariant ?? ThemeVariant.Default,
            out var theme
        );

        // gets the color from the theme
        theme?.TryGetResource(
            appColor.ToString(),
            Application.Current?.ActualThemeVariant ?? ThemeVariant.Default,
            out color
        );

        // if the AppColors.axaml has a Color value instead of SolidColorBrush it returns accordingly
        if (color is Color extractedColor)
        {
            return new ImmutableSolidColorBrush(extractedColor);
        }

        return color as ImmutableSolidColorBrush ?? defaultColorBrush;

    }
}
