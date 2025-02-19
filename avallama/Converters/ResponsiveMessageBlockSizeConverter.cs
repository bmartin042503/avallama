using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace avallama.Converters;

public class ResponsiveMessageBlockSizeConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double windowWidth)
        {
            return windowWidth / 2.25;
        }

        return 400;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}