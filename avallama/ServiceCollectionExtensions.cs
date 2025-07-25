// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

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
        
        // ciklikus függőségre vigyázni kell, mert megeshet hogy nem dob kivételt, nem hoz létre semmit de mégis fut az alkalmazás
        // és nehezen lehet debuggolni, pl. AppService -> MainViewModel -> PageFactory -> Func<...> -> HomeViewModel -> AppService
        
        // TODO: ezeket majd talán jobban "interfészesíteni" hogy tesztelésnél könnyebb legyen idk
        
        // gyenge referenciás messenger, ami azt jelenti hogy nem kell manuálisan törölni őket
        collection.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);
        
        collection.AddTransient<DatabaseInitService>();
        collection.AddSingleton<DatabaseService>();

        collection.AddSingleton<OllamaService>();
        collection.AddSingleton<DialogService>();
        collection.AddSingleton<LocalizationService>();
        collection.AddSingleton<ConfigurationService>();
        
        collection.AddSingleton<MainViewModel>();
        collection.AddTransient<HomeViewModel>();
        collection.AddTransient<GreetingViewModel>();
        collection.AddTransient<SettingsViewModel>();
        collection.AddTransient<ModelManagerViewModel>();
        collection.AddTransient<GuideViewModel>();

        collection.AddSingleton<PageFactory>();
        collection.AddSingleton<DialogViewModelFactory>();
        
        collection.AddSingleton<IAppService, AppService>();

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

        collection.AddSingleton<Func<ApplicationDialog, DialogViewModel>>(serviceProvider => content => content switch
        {
            ApplicationDialog.Settings => serviceProvider.GetRequiredService<SettingsViewModel>(),
            ApplicationDialog.ModelManager => serviceProvider.GetRequiredService<ModelManagerViewModel>(),
            _ => throw new InvalidOperationException() 
            // Info, Error és a többi dialog nem kell, mert azok nem hívják meg ezt
        });

    }
}