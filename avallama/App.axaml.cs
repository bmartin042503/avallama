// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using System.Threading.Tasks;
using avallama.Constants;
using avallama.Extensions;
using Avalonia.Markup.Xaml;
using avallama.Services;
using avallama.Services.Ollama;
using avallama.Services.Persistence;
using Avalonia.Controls;
using Avalonia.Styling;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace avallama;

public partial class App : Application
{
    private OllamaService? _ollamaService;
    private DialogService? _dialogService;
    private DatabaseInitService? _databaseInitService;
    public static SqliteConnection SharedDbConnection { get; private set; } = null!;
    public const string Version = "v0.3.0";

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Create all dependencies and store them in a ServiceCollection
        var collection = new ServiceCollection();
        collection.AddCommonServices();

        // ServiceProvider which provides the created dependencies
        var services = collection.BuildServiceProvider();

        // there cannot be any dependency requests before ConfigurationService
        // otherwise localized texts, colors will not be displayed correctly
        // localization is set based on the system language, this needs to be overridden by ConfigurationService

        // language and theme query, setting
        var configurationService = services.GetRequiredService<ConfigurationService>();

        var colorScheme = configurationService.ReadSetting(ConfigurationKey.ColorScheme);
        var language = configurationService.ReadSetting(ConfigurationKey.Language);

        var showInformationalMessages = configurationService.ReadSetting(ConfigurationKey.IsInformationalMessagesVisible);

        // the information message is enabled by default for new users
        if (string.IsNullOrWhiteSpace(showInformationalMessages))
        {
            configurationService.SaveSetting(ConfigurationKey.IsInformationalMessagesVisible, true.ToString());
        }

        var cultureInfo = language switch
        {
            "hungarian" => CultureInfo.GetCultureInfo("hu-HU"),
            _ => CultureInfo.InvariantCulture
        };

        RequestedThemeVariant = colorScheme switch
        {
            "dark" => ThemeVariant.Dark,
            "light" => ThemeVariant.Light,
            _ => ThemeVariant.Default
        };

        LocalizationService.ChangeLanguage(cultureInfo);

        var appService = services.GetRequiredService<IApplicationService>();
        _ollamaService = services.GetRequiredService<OllamaService>();
        _dialogService = services.GetRequiredService<DialogService>();
        _databaseInitService = services.GetRequiredService<DatabaseInitService>();
        SharedDbConnection = _databaseInitService.InitializeDatabaseAsync().GetAwaiter().GetResult();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // subscribe to OnStartup and OnExit events
            desktop.Startup += OnStartup;
            desktop.Exit += OnExit;
            desktop.ShutdownMode = ShutdownMode.OnMainWindowClose;

            // Avoid duplicate validations from both Avalonia and the CommunityToolkit.
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();
            appService.InitializeMainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void OnStartup(object? sender, ControlledApplicationLifetimeStartupEventArgs e)
    {
        // runs on a separate thread, asynchronously in theory
        // not likely that any exception would be caught but just in case
        Task.Run(async () =>
        {
            try
            {
                await _ollamaService!.Start();
            }
            catch (Exception)
            {
                // TODO: proper logging
            }
        });
    }

    private void OnExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        _ollamaService?.Stop();
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }

    private void About_OnClick(object? sender, EventArgs e)
    {
        // this is for later for some fancy dialog
        _dialogService?.ShowInfoDialog(
            "Avallama - " + Version
                          + "\n\nCopyright (c) " + LocalizationService.GetString("DEVELOPER_NAMES")
                          + "\n\n" + LocalizationService.GetString("LICENSE_DETAILS")
                          + "\n\n" + LocalizationService.GetString("FROM_ORG") + " (github.com/4foureyes/avallama)"
        );
    }
}
