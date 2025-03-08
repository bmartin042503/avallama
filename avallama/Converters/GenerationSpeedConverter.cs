﻿// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Globalization;
using avallama.Services;
using Avalonia.Data.Converters;

namespace avallama.Converters;

public class GenerationSpeedConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double generationSpeed and > 0.0)
        {
            return $"{generationSpeed} {LocalizationService.GetString("TOKEN_SEC")}";
        }
        return LocalizationService.GetString("GENERATING_MESSAGE");
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}