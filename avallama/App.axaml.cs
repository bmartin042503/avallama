using System;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Markup.Xaml;
using avallama.ViewModels;
using avallama.Views;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace avallama;

public partial class App : Application
{
    private Process? _ollamaProcess;
    
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // TODO: beállítani user preference szerint a nyelvet
        // beállítás alapértelmezett culturera (angol nyelv)
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
        
        // Az összes dependency létrehozása és eltárolása egy ServiceCollectionben
        var collection = new ServiceCollection();
        collection.AddCommonServices();
        
        // ServiceProvider ami biztosítja a létrehozott dependencyket
        var services = collection.BuildServiceProvider();
        
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
        StartOllamaServiceWithDelay(TimeSpan.FromSeconds(1));
    }

    private void OnExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        StopOllamaService();
    }
    
    private async void StartOllamaServiceWithDelay(TimeSpan delay)
    {
        await Task.Delay(delay);

        StartOllamaService();
    }
    
    private void StartOllamaService()
    {
        // ez eddig csak Windows specifikus
        string ollamaPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            @"Programs\Ollama\ollama"
        );
        
        var startInfo = new ProcessStartInfo
        {
            FileName = ollamaPath, 
            Arguments = "serve",
            UseShellExecute = false,
            CreateNoWindow = true
        };

        try
        {
            _ollamaProcess = Process.Start(startInfo);
        }
        catch (Exception ex)
        {
            // ehelyett ki lehetne iratni valamit UI-ra
            Console.WriteLine($"Error starting Ollama service: {ex.Message}");
        }

        if (_ollamaProcess != null)
        {
            // ehelyett is lehetne a UI-on valami pipa ami jelezze hogy sikeresen elindult a service
            Console.WriteLine($"Started service: {_ollamaProcess.ProcessName}");
        }
    }
    
    private void StopOllamaService()
    {
        if (_ollamaProcess != null && !_ollamaProcess.HasExited)
        {
            _ollamaProcess.Kill();
            _ollamaProcess.Dispose();
        }
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