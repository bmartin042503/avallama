using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Markup.Xaml;
using avallama.ViewModels;
using avallama.Views;
using avallama.Services;
using Microsoft.Extensions.DependencyInjection;

namespace avallama;

public partial class App : Application
{
    private OllamaService? _ollamaService;
    
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
        // CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
        
        // Az összes dependency létrehozása és eltárolása egy ServiceCollectionben
        var collection = new ServiceCollection();
        collection.AddCommonServices();
        
        // ServiceProvider ami biztosítja a létrehozott dependencyket
        var services = collection.BuildServiceProvider();
        
        _ollamaService = services.GetRequiredService<OllamaService>();
        
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            //feliratkozunk az OnStartup-ra és az OnExitre
            desktop.Startup += OnStartup;
            desktop.Exit += OnExit;
            
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();
            desktop.MainWindow = new MainWindow
            {
                DataContext = services.GetRequiredService<MainViewModel>()
            };
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
}