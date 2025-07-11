// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using avallama.Constants;
using Avalonia.Markup.Xaml;
using avallama.Services;
using avallama.ViewModels;
using avallama.Views;
using Avalonia.Controls;
using Avalonia.Styling;
using Microsoft.Extensions.DependencyInjection;

namespace avallama;

public partial class App : Application
{
    private OllamaService? _ollamaService;
    private DialogService? _dialogService;
    
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Az összes dependency létrehozása és eltárolása egy ServiceCollectionben
        var collection = new ServiceCollection();
        collection.AddCommonServices();
        
        // ServiceProvider ami biztosítja a létrehozott dependencyket
        var services = collection.BuildServiceProvider();
        
        _ollamaService = services.GetRequiredService<OllamaService>();
        _dialogService = services.GetRequiredService<DialogService>();
        
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var configurationService = services.GetRequiredService<ConfigurationService>();
            
            var colorScheme = configurationService.ReadSetting("color-scheme");
            var language = configurationService.ReadSetting("language");

            RequestedThemeVariant = colorScheme switch
            {
                "dark" => ThemeVariant.Dark,
                "light" => ThemeVariant.Light,
                _ => ThemeVariant.Default
            };

            var cultureInfo = language switch
            {
                "hungarian" => CultureInfo.GetCultureInfo("hu-HU"),
                "english" => CultureInfo.InvariantCulture,
                _ => CultureInfo.InvariantCulture
            };
            
            var localizationService = services.GetRequiredService<LocalizationService>();
            localizationService.ChangeLanguage(cultureInfo);
            
            //feliratkozunk az OnStartup-ra és az OnExitre
            desktop.Startup += OnStartup;
            desktop.Exit += OnExit;
            desktop.ShutdownMode = ShutdownMode.OnMainWindowClose;
            
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();

            // saját app launcher hogy először megjelenhessen a dialog és utána a MainWindow
            var launcher = services.GetRequiredService<AppLauncherService>();
            launcher.Run();
        }

        base.OnFrameworkInitializationCompleted();
    }
    
    private void OnStartup(object? sender, ControlledApplicationLifetimeStartupEventArgs e)
    {
        /*
         ide kell egy kis delay kulonben elobb inditja a servicet mint hogy betoltene a UI-t es ugy nem igazan
         lehet kiirni a hibakat UI-ra
        */
        _ollamaService?.StartWithDelay(TimeSpan.FromSeconds(2));
    }

    private void OnExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        OllamaService.Stop();
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
        // ezt később majd vmi fancy dialogra
        _dialogService?.ShowInfoDialog("Avallama - " + LocalizationService.GetString("VERSION"));
    }
}