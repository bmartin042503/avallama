// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;

namespace avallama.Views;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
        
        // traffic light window gombok miatt lentebb kell tolni macen
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            PageTitle.Margin = new Thickness(0,10,0,6);
        }
    }

    private void SaveBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        // sötét/világos témaváltásnál a datacontextet frissíti, egyelőre csak így működik
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime applicationLifetime) return;
        var tmpDataContext = applicationLifetime.MainWindow!.FindControl<ContentControl>("PageContent")!.DataContext;
        applicationLifetime.MainWindow!.FindControl<ContentControl>("PageContent")!.DataContext = null;
        applicationLifetime.MainWindow!.FindControl<ContentControl>("PageContent")!.DataContext = tmpDataContext;
    }
}