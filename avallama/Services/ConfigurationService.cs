using System;
using System.Configuration;
using System.Globalization;
using Avalonia.Styling;

namespace avallama.Services;

public class ConfigurationService(LocalizationService localizationService)
{
    private readonly LocalizationService? _localizationService = localizationService;

    public string ReadSetting(string key)
    {
        try
        {
            var appSettings = ConfigurationManager.AppSettings;
            return appSettings[key] ?? string.Empty;
        }
        catch (ConfigurationErrorsException e)
        {
            Console.WriteLine(e.Message);
        }
        return string.Empty;
    }

    public void SaveSetting(string key, string value)
    {
        try
        {
            var configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            var settings = configFile.AppSettings.Settings;
            if (settings[key] == null)
            {
                settings.Add(key, value);
            }
            else
            {
                settings[key].Value = value;
            }
            configFile.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection(configFile.AppSettings.SectionInformation.Name);
            if (Avalonia.Application.Current is not App app) return;
            switch (key)
            {
                case "color-scheme":
                    app.RequestedThemeVariant = value == "light" ? ThemeVariant.Light : ThemeVariant.Dark;
                    break;
                case "language":
                    _localizationService?.ChangeLanguage(value == "hungarian"
                        ? CultureInfo.GetCultureInfo("hu-HU")
                        : CultureInfo.InvariantCulture);
                    break;
            }
        }
        catch (ConfigurationErrorsException e)
        {
            Console.WriteLine(e.Message);
        }
    }
}