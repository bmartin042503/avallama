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
using Avalonia.Markup.Xaml;
using avallama.Services;
using avallama.Utilities;
using avallama.ViewModels;
using avallama.Views;
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
        _databaseInitService = services.GetRequiredService<DatabaseInitService>();
        SharedDbConnection = Task.Run(() => _databaseInitService.GetOpenConnectionAsync()).GetAwaiter().GetResult();
        
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var configurationService = services.GetRequiredService<ConfigurationService>();
            
            var colorScheme = configurationService.ReadSetting(ConfigurationKey.ColorScheme);
            var language = configurationService.ReadSetting(ConfigurationKey.Language);

            RequestedThemeVariant = colorScheme switch
            {
                "dark" => ThemeVariant.Dark,
                "light" => ThemeVariant.Light,
                _ => ThemeVariant.Default
            };

            var cultureInfo = language switch
            {
                "hungarian" => CultureInfo.GetCultureInfo("hu-HU"),
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

            var launcher = services.GetRequiredService<IAppService>();
            launcher.InitializeMainWindow();
            
            // igaz hogy configurationben már meg van adva hogy localhost és ha az nem az, akkor lehet tudni hogy remote
            // de kell egy másik configuration key arra is, hogy már megválaszolta-e ezt a kérdést, így nem dobja fel megint
            var startOllamaFrom = configurationService.ReadSetting(ConfigurationKey.StartOllamaFrom);
        
            var firstTime = configurationService.ReadSetting(ConfigurationKey.FirstTime);

            // ez csak akkor fut le ha már a felhasználó végigment a greeting screenen de valami miatt még nem válaszolt a kérdésre
            // technikailag új felhasználóknak soha nem futna le ez, but who knows
            if (string.IsNullOrEmpty(startOllamaFrom) && firstTime == "false")
            {
                _ = launcher.CheckOllamaStart();
            }
        }
        base.OnFrameworkInitializationCompleted();
    }
    
    private void OnStartup(object? sender, ControlledApplicationLifetimeStartupEventArgs e)
    {
        // külön szálon, aszinkron fut le elvileg
        // nem valószínű hogy elkapna valaha is bármilyen kivételt de biztos ami biztos
        // gondoltam mivel elég alapvető service, nem lenne helyes egy soros _ = _ollamaService?.Start()-al letudni xd
        _ = Task.Run(async () =>
        {
            try
            {
                await _ollamaService!.Start();
            }
            catch (Exception ex)
            {
                // TODO: ehelyett majd logolás
                Console.WriteLine(ex);
            }
        });
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