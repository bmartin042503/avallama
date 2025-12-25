// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Globalization;
using avallama.Constants;
using avallama.Services;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace avallama.Converters;

// Converts the sorting option enum to localized string, so that these don't have to be used in the background
public class SortingOptionConverter : IValueConverter
{

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is SortingOption sortingOption)
        {
            return sortingOption switch
            {
                SortingOption.Downloaded => LocalizationService.GetString("SORT_DOWNLOADED"),
                SortingOption.PullCountAscending => LocalizationService.GetString("SORT_PULL_COUNT_ASCENDING"),
                SortingOption.PullCountDescending => LocalizationService.GetString("SORT_PULL_COUNT_DESCENDING"),
                SortingOption.SizeAscending => LocalizationService.GetString("SORT_SIZE_ASCENDING"),
                SortingOption.SizeDescending => LocalizationService.GetString("SORT_SIZE_DESCENDING"),
                _ => null
            };
        }
        return null;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return new BindingNotification(new NotSupportedException("SortingOption value cannot be converted back."));
    }
}
