// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System.Runtime.InteropServices;
using avallama.Constants;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace avallama.Views;

public partial class DialogWindow : Window
{

    public DialogWindow()
    {
        InitializeComponent();
        SizeToContent = SizeToContent.WidthAndHeight;
    }
    
    private void Window_PointerPressed(object? sender, RoutedEventArgs e)
    {
        // Linuxon még nem vagyok benne biztos hogy eltűnne az ablak felső része
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            var args = e as PointerPressedEventArgs;
            if (args == null) return;
            var positionY = args.GetPosition(this).Y;

            // ablakot csak Y:30 alatt lehet mozgatni, vagyis az ablak felső részén
            if (positionY < 30) BeginMoveDrag((PointerPressedEventArgs)e);
        }
    }
}