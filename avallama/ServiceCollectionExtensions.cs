// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using avallama.Constants;
using avallama.ViewModels;
using avallama.Factories;
using avallama.Services;
using avallama.Views;
using Microsoft.Extensions.DependencyInjection;

namespace avallama;

public static class ServiceCollectionExtensions
{
    public static void AddCommonServices(this IServiceCollection collection)
    {
        // Dependencyk létrehozása
        // Singleton - Memóriában folytonosan jelen van
        // Transient - Csak akkor hozza létre amikor szükség van rá és ha nincs akkor törli
        
        collection.AddSingleton<OllamaService>();
        collection.AddSingleton<DialogService>();
        collection.AddSingleton<LocalizationService>();
        collection.AddSingleton<ConfigurationService>();
        
        collection.AddSingleton<MainViewModel>();
        collection.AddTransient<HomeViewModel>();
        collection.AddTransient<GreetingViewModel>();
        collection.AddTransient<SettingsViewModel>();
        collection.AddSingleton<OllamaServiceViewModel>();
        collection.AddTransient<GuideViewModel>();

        collection.AddSingleton<PageFactory>();
        collection.AddSingleton<DialogViewModelFactory>();
        
        collection.AddTransient<DialogWindow>();
        
        collection.AddSingleton<AppLauncherService>();

        // PageFactoryba injektálandó delegate dependency
        // ez biztosítja hogy az App.axaml.cs-ben lesz minden dependency kezelve a factory pattern szerint
        // Func<ApplicationPage, PageViewModel> - adott ApplicationPage-re vissza ad egy PageViewModelt
        collection.AddSingleton<Func<ApplicationPage, PageViewModel>>(serviceProvider => page => page switch
        {
            ApplicationPage.Greeting => serviceProvider.GetRequiredService<GreetingViewModel>(),
            ApplicationPage.Home => serviceProvider.GetRequiredService<HomeViewModel>(),
            ApplicationPage.Guide => serviceProvider.GetRequiredService<GuideViewModel>(),
            _ => throw new InvalidOperationException() // ha még nincs adott Page regisztrálva akkor kivétel
        });

        collection.AddSingleton<Func<ApplicationDialogContent, DialogViewModel>>(serviceProvider => content => content switch
        {
            ApplicationDialogContent.Settings => serviceProvider.GetRequiredService<SettingsViewModel>(),
            ApplicationDialogContent.OllamaService => serviceProvider.GetRequiredService<OllamaServiceViewModel>(),
            _ => throw new InvalidOperationException()
        });

    }
}