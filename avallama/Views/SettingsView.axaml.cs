// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System.Runtime.InteropServices;
using avallama.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;

namespace avallama.Views;

public partial class SettingsView : UserControl
{
    private int? _selectedThemeIndex;
    
    public SettingsView()
    {
        InitializeComponent();
        
        // traffic light window gombok miatt lentebb kell tolni macen
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            PageTitle.Margin = new Thickness(0,10,0,20);
        }
    }

    protected override void OnInitialized()
    {
        // ha a SettingsView végzett az inicializálásnál akkor elmentjük a jelenleg kiválasztott színséma indexét
        // ezt azért kell meghívni itt, mert az inicializálás végén lesz beállítva a SettingsView DataContextje, vagyis a SettingsViewModel
        // és a SettingsViewModelből kérjük le az indexet
        // lehetne amúgy simán a UI elemről is lekérni, de mivel Binding van így úgy gondoltam a viewmodelből lekérve naprakészebb
        _selectedThemeIndex = GetThemeIndex();
    }

    private void SaveBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        // ha változott a kiválasztott színséma, akkor datacontext frissítés, ezzel lehet váltani a színeket újraindítás nélkül
        var vmThemeIndex = GetThemeIndex();
        if (_selectedThemeIndex == vmThemeIndex) return;
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime applicationLifetime) return;
        var tmpDataContext = applicationLifetime.MainWindow!.FindControl<ContentControl>("PageContent")!.DataContext;
        applicationLifetime.MainWindow!.FindControl<ContentControl>("PageContent")!.DataContext = null;
        applicationLifetime.MainWindow!.FindControl<ContentControl>("PageContent")!.DataContext = tmpDataContext;
    }

    private int? GetThemeIndex()
    {
        if (DataContext is SettingsViewModel vm)
        {
            return vm.SelectedThemeIndex;
        }

        return null;
    }
}