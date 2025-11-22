// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Globalization;
using System.Text.RegularExpressions;
using avallama.Services;

namespace avallama.Utilities;

/// <summary>
/// Provides helper methods for converting abbreviated numbers,
/// size strings (e.g., "2.5GB") to bytes, and bytes to formatted sizes.
/// </summary>
public static class ConversionHelper
{
    private const double Thousand = 1_000d;
    private const double Million = 1_000_000d;
    private const double Billion = 1_000_000_000d;

    private const double Kb = 1_000d;
    private const double Mb = 1_000_000d;
    private const double Gb = 1_000_000_000d;
    private const double Tb = 1_000_000_000_000d;
    private const double Pb = 1_000_000_000_000_000d;

    private static readonly Regex SizeRegex =
        new(@"^([0-9]*\.?[0-9]+)\s*(B|KB|MB|GB|TB|PB)?$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// Parses an abbreviated numeric string such as "1.5M", "120K", or "2B"
    /// into its numeric value (e.g., 1500000).
    /// </summary>
    /// <param name="input">The abbreviated number string.</param>
    /// <returns>The expanded numeric value, or 0 if parsing fails.</returns>
    public static long ParseAbbreviatedNumber(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return 0;

        input = input.Trim().ToUpperInvariant();

        double multiplier = 1;

        if (input.EndsWith('B'))
        {
            multiplier = Billion;
            input = input[..^1];
        }
        else if (input.EndsWith('M'))
        {
            multiplier = Million;
            input = input[..^1];
        }
        else if (input.EndsWith('K'))
        {
            multiplier = Thousand;
            input = input[..^1];
        }

        return double.TryParse(input, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out double value)
            ? (long)(value * multiplier)
            : 0;
    }

    /// <summary>
    /// Converts a long number into an abbreviated numeric string like "1.5M", "120K", or "2B".
    /// </summary>
    /// <param name="number">The number to abbreviate.</param>
    /// <returns>The abbreviated string representation.</returns>
    public static string FormatToAbbreviatedNumber(long number)
    {
        double value = number;
        var suffix = "";

        if (Math.Abs(number) >= Billion)
        {
            value = number / Billion;
            suffix = "B";
        }
        else if (Math.Abs(number) >= Million)
        {
            value = number / Million;
            suffix = "M";
        }
        else if (Math.Abs(number) >= Thousand)
        {
            value = number / Thousand;
            suffix = "K";
        }

        var formatted = value % 1 == 0
            ? ((long)value).ToString()
            : value.ToString("0.#", CultureInfo.InvariantCulture);

        return formatted + suffix;
    }

    public static long ParseSizeToBytes(string size)
    {
        if (string.IsNullOrWhiteSpace(size))
            return 0;

        size = size.Trim().ToUpperInvariant();

        var match = SizeRegex.Match(size);
        if (!match.Success)
            return 0;

        var value = double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
        var unit = match.Groups[2].Success ? match.Groups[2].Value.ToUpperInvariant() : "B";

        var multiplier = unit switch
        {
            "B" => 1,
            "KB" => Kb,
            "MB" => Mb,
            "GB" => Gb,
            "TB" => Tb,
            "PB" => Pb,
            _ => 0
        };

        return (long)(value * multiplier);
    }

    /// <summary>
    /// Converts a byte value into a formatted gigabyte (GB) string using
    /// SI units (1 GB = 1,000,000,000 bytes).
    /// </summary>
    /// <param name="bytes">The size in bytes.</param>
    /// <returns>A formatted string based on the current localization.</returns>
    public static string FormatSizeInGb(long bytes)
    {
        var gbValue = bytes / Gb;
        var rounded = Math.Round(gbValue, 1);

        var displayValue = rounded % 1 == 0
            ? ((int)rounded).ToString(CultureInfo.InvariantCulture)
            : rounded.ToString("0.00", CultureInfo.InvariantCulture);

        return string.Format(
            LocalizationService.GetString("SIZE_IN_GB"),
            displayValue
        );
    }

    /// <summary>
    /// Parses a date string formatted as "MMM d, yyyy h:mm tt UTC"
    /// (e.g. "Dec 5, 2023 11:59 PM UTC") into a UTC <see cref="DateTime"/>.
    /// </summary>
    /// <param name="input">The UTC timestamp string to parse.</param>
    /// <returns>A <see cref="DateTime"/> in UTC.</returns>
    public static DateTime ParseUtcDate(string input)
    {
        const string format = "MMM d, yyyy h:mm tt 'UTC'";

        return DateTime.ParseExact(
            input,
            format,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal
        );
    }
}
