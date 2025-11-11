// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Globalization;
using System.Text.RegularExpressions;
using avallama.Services;

namespace avallama.Utilities;

public class ConversionHelper
{
    public long ParsePullCount(string s)
    {
        // e.g., "1.5M", "120K", "100", "2.3B" etc.
        s = s.Trim().ToUpperInvariant();
        double multiplier = 1;
        if (s.EndsWith('B'))
        {
            multiplier = 1_000_000_000;
            s = s[..^1];
        }
        else if (s.EndsWith('M'))
        {
            multiplier = 1_000_000;
            s = s[..^1];
        }
        else if (s.EndsWith('K'))
        {
            multiplier = 1_000;
            s = s[..^1];
        }

        if (double.TryParse(s, System.Globalization.NumberStyles.AllowDecimalPoint,
                System.Globalization.CultureInfo.InvariantCulture, out double value))
        {
            return (long)(value * multiplier);
        }

        return 0;
    }

    public static long ParseSizeTextToBytes(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return 0;

        input = input.Trim().ToUpperInvariant();

        var match = Regex.Match(input, @"^([0-9]*\.?[0-9]+)\s*(B|KB|MB|GB|TB|PB)?$");
        if (!match.Success) return 0;

        var value = double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
        var unit = match.Groups[2].Success ? match.Groups[2].Value : "B";

        var multiplier = unit switch
        {
            "B"  => 1,
            "KB" => Math.Pow(1000, 1),
            "MB" => Math.Pow(1000, 2),
            "GB" => Math.Pow(1000, 3),
            "TB" => Math.Pow(1000, 4),
            "PB" => Math.Pow(1000, 5),
            _ => 0
        };

        var bytes = value * multiplier;

        return Convert.ToInt64(bytes);
    }

    public static string GetSizeInGb(long sizeInBytes)
    {
        var sizeInGb = sizeInBytes / (1024.0 * 1024.0 * 1024.0);
        var rounded = Math.Round(sizeInGb, 1);

        // ha a tizedesjegy nulla, ne jelenjen meg
        var displayValue = rounded % 1 == 0
            ? ((int)rounded).ToString()
            : rounded.ToString("0.0", CultureInfo.InvariantCulture);

        return string.Format(LocalizationService.GetString("SIZE_IN_GB"), displayValue);
    }
}
