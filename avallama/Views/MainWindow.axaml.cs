// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace avallama.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Windows specific settings

            // Remove title bar
            ExtendClientAreaToDecorationsHint = true;
            ExtendClientAreaChromeHints = Avalonia.Platform.ExtendClientAreaChromeHints.NoChrome;
            ExtendClientAreaTitleBarHeightHint = 0;
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            // Remove title bar on macOS, but keep window control buttons in the top left corner
            ExtendClientAreaToDecorationsHint = true;
            ReplaceCanvasWithGrid();
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            ReplaceCanvasWithGrid();
        }
    }

    private void ReplaceCanvasWithGrid()
    {
        // new grid, which will be the Window content, and its only child is the ContentControl in MainWindow.axaml
        var grid = new Grid();
        var contentControl = MainCanvas.Children[1];
        MainCanvas.Children.Clear();
        grid.Children.Add(contentControl);
        Content = grid;
    }

    private void Window_PointerPressed(object? sender, RoutedEventArgs e)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            var args = e as PointerPressedEventArgs;
            if (args == null) return;
            var positionY = args.GetPosition(this).Y;

            // the window can only be moved under Y:30, so the top part of the window
            if (positionY < 30) BeginMoveDrag((PointerPressedEventArgs)e);
        }
    }

    private void CloseButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void MinMaxButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var svgIcon = this.FindControl<Avalonia.Svg.Svg>("MinMaxSvg");
        if (WindowState == WindowState.Maximized)
        {
            WindowState = WindowState.Normal;
            if(svgIcon != null) svgIcon.Path = "/Assets/Svg/maximize.svg";
        }
        else
        {
            WindowState = WindowState.Maximized;
            if(svgIcon != null) svgIcon.Path = "/Assets/Svg/minimize.svg";
        }
    }

    private void HideButton_OnClick(object? sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }
}
