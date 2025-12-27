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
    private int _selectedThemeIndex;

    public SettingsView()
    {
        InitializeComponent();

        // needs to be pushed down on macOS because of the traffic light window buttons
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            PageTitle.Margin = new Thickness(0,10,0,20);
        }
    }

    protected override void OnInitialized()
    {
        // if SettingsView initialization is complete, then we save the currently selected theme index
        // this needs to be called here, because at the end of the initialization the DataContext of the SettingsView will be set, i.e. the SettingsViewModel
        // and we get the index from the SettingsViewModel
        // it could be queried from the UI element as well, but since there is Binding, I thought it would be more up-to-date if queried from the viewmodel
        _selectedThemeIndex = GetThemeIndex();
    }

    private void SaveBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        // if the selected theme changes, then refresh datacontext, so the colors can be updated without restarting the app
        var vmThemeIndex = GetThemeIndex();
        if (_selectedThemeIndex == vmThemeIndex) return;
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime applicationLifetime) return;
        var tmpDataContext = applicationLifetime.MainWindow!.FindControl<ContentControl>("PageContent")!.DataContext;
        applicationLifetime.MainWindow!.FindControl<ContentControl>("PageContent")!.DataContext = null;
        applicationLifetime.MainWindow!.FindControl<ContentControl>("PageContent")!.DataContext = tmpDataContext;
    }

    private int GetThemeIndex()
    {
        if (DataContext is SettingsViewModel vm)
        {
            return vm.SelectedThemeIndex;
        }

        return 0;
    }
}
