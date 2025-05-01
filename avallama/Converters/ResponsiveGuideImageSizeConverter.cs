using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace avallama.Converters;

public class ResponsiveGuideImageSizeConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double controlHeight)
        {
            return controlHeight / 1.5;
        }

        return 500;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}