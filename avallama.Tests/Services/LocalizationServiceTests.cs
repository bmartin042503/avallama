// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System.Globalization;
using avallama.Services;
using Xunit;

namespace avallama.Tests.Services;

public class LocalizationServiceTests
{
    private readonly CultureInfo _hungarianCultureInfo = new ("hu-HU");
    private readonly CultureInfo _defaultCultureInfo = CultureInfo.InvariantCulture; // English

    [Fact]
    public void GetString_WithDefinedLocalizationKey_ReturnsCorrectLocalizedValue()
    {
        const string key = "TEST";

        LocalizationService.ChangeLanguage(_hungarianCultureInfo);
        var hungarianLocalizedText = LocalizationService.GetString(key);

        LocalizationService.ChangeLanguage(_defaultCultureInfo);
        var defaultLocalizedText = LocalizationService.GetString(key);

        Assert.Equal("Lokalizált értékek tesztelése", hungarianLocalizedText);
        Assert.Equal("Testing localization values", defaultLocalizedText);
    }

    [Fact]
    public void GetString_WithUndefinedLocalizationKey_ReturnsUndefinedValue()
    {
        const string key = "THIS_LOCALIZATION_KEY_IS_UNDEFINED";

        LocalizationService.ChangeLanguage(_hungarianCultureInfo);
        var hungarianLocalizedText = LocalizationService.GetString(key);

        LocalizationService.ChangeLanguage(_defaultCultureInfo);
        var defaultLocalizedText = LocalizationService.GetString(key);

        Assert.Equal("[UNDEFINED_LOCALIZATION_KEY]", hungarianLocalizedText);
        Assert.Equal("[UNDEFINED_LOCALIZATION_KEY]", defaultLocalizedText);
    }
}
