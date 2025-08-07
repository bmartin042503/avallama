// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Styling;

namespace avallama.Utilities;

public enum AppColor
{
    Primary,
    SurfaceTint,
    OnPrimary,
    PrimaryContainer,
    OnPrimaryContainer,
    Secondary,
    OnSecondary,
    SecondaryContainer,
    OnSecondaryContainer,
    Tertiary,
    OnTertiary,
    TertiaryContainer,
    OnTertiaryContainer,
    Error,
    OnError,
    ErrorContainer,
    OnErrorContainer,
    Background,
    OnBackground,
    Surface,
    OnSurface,
    SurfaceVariant,
    OnSurfaceVariant,
    Outline,
    OutlineVariant,
    Shadow,
    Scrim,
    InverseSurface,
    InverseOnSurface,
    InversePrimary,
    PrimaryFixed,
    OnPrimaryFixed,
    PrimaryFixedDim,
    OnPrimaryFixedVariant,
    SecondaryFixed,
    OnSecondaryFixed,
    SecondaryFixedDim,
    OnSecondaryFixedVariant,
    TertiaryFixed,
    OnTertiaryFixed,
    TertiaryFixedDim,
    OnTertiaryFixedVariant,
    SurfaceDim,
    SurfaceBright,
    SurfaceContainerLowest,
    SurfaceContainerLow,
    SurfaceContainer,
    SurfaceContainerHigh,
    SurfaceContainerHighest
}


// a témának megfelelő színt adja vissza a szín kulcsának alapján
public static class ColorProvider
{
    public static ImmutableSolidColorBrush GetColor(AppColor appColor)
    {
        var defaultColorBrush = new ImmutableSolidColorBrush(Colors.Black);
        
        object? color = defaultColorBrush;
        var themes = Application.Current?.Resources.ThemeDictionaries;
        if (themes is null) return defaultColorBrush;

        // az app themei közül kiszedi a Lightot és a Darkot majd a theme-ben visszaadja azt amelyik használatban van
        themes.TryGetValue(
            Application.Current?.ActualThemeVariant ?? ThemeVariant.Default,
            out var theme
        );
        
        // a themeből kiszedi a színt
        theme?.TryGetResource(
            appColor.ToString(),
            Application.Current?.ActualThemeVariant ?? ThemeVariant.Default,
            out color
        );
        
        return color as ImmutableSolidColorBrush ?? defaultColorBrush;
    }
}