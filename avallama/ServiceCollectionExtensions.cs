using System;
using avallama.Constants;
using avallama.ViewModels;
using avallama.Factories;
using avallama.Services;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace avallama;

public static class ServiceCollectionExtensions
{
    public static void AddCommonServices(this IServiceCollection collection)
    {
        // Dependencyk létrehozása
        // Singleton - Memóriában folytonosan jelen van
        // Transient - Csak akkor hozza létre amikor szükség van rá és ha nincs akkor törli
        
        collection.AddSingleton<MainViewModel>();
        collection.AddTransient<GreetingViewModel>();
        collection.AddTransient<HomeViewModel>();

        collection.AddSingleton<PageFactory>();
        collection.AddSingleton<OllamaService>();
        collection.AddSingleton<PerformanceService>();

        // PageFactoryba injektálandó delegate dependency
        // ez biztosítja hogy az App.axaml.cs-ben lesz minden dependency kezelve a factory pattern szerint
        // Func<ApplicationPage, PageViewModel> - adott ApplicationPage-re vissza ad egy PageViewModelt
        collection.AddSingleton<Func<ApplicationPage, PageViewModel>>(serviceProvider => page => page switch
        {
            ApplicationPage.Greeting => serviceProvider.GetRequiredService<GreetingViewModel>(),
            ApplicationPage.Home => serviceProvider.GetRequiredService<HomeViewModel>(),
            _ => throw new InvalidOperationException() // ha még nincs adott Page regisztrálva akkor kivétel
        });
        
        
        
        
    }
}