// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;

namespace avallama.Views.Dialogs;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
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