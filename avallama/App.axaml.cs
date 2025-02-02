using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Markup.Xaml;
using avallama.ViewModels;
using avallama.Views;
using System.Globalization;
using Microsoft.Extensions.DependencyInjection;

namespace avallama;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // TODO: kiszedni az alapértelmezett (angol) culturet és beállítani user preference szerint
        // CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
        // CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
        
        // Az összes dependency létrehozása és eltárolása egy ServiceCollectionben
        var collection = new ServiceCollection();
        collection.AddCommonServices();
        
        // ServiceProvider ami biztosítja a létrehozott dependencyket
        var services = collection.BuildServiceProvider();
        
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
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