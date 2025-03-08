// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Globalization;
using avallama.Services;
using Avalonia.Data.Converters;

namespace avallama.Converters;

public class IconButtonTextConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var windowWidth = (double)(value ?? 0.0);
        return windowWidth switch
        {
            0.0 or < 1650 => LocalizationService.GetString("NEW"),
            >= 1650 => LocalizationService.GetString("NEW_CONVERSATION"),
            _ => LocalizationService.GetString("NEW")
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}