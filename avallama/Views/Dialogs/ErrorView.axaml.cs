// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace avallama.Views.Dialogs;

public partial class ErrorView : UserControl
{
    public ErrorView()
    {
        InitializeComponent();
    }
    
    private void CloseButton_Click(object? sender, RoutedEventArgs e)
    {
        var window = this.GetVisualAncestors().OfType<Window>().FirstOrDefault();
        window?.Close();
    }
}