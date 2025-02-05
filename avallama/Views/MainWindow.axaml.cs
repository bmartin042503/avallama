using System;
using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace avallama.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        // Title bar eltüntetése (csak Windowson)
        // Linuxon sok desktop environment van, nem biztos hogy általánosan mindegyikről el tudnánk tüntetni
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            ExtendClientAreaToDecorationsHint = true;
            ExtendClientAreaChromeHints = Avalonia.Platform.ExtendClientAreaChromeHints.NoChrome;
            ExtendClientAreaTitleBarHeightHint = 0;
        }
        InitializeComponent();
    }
    
    private void Window_PointerPressed(object? sender, RoutedEventArgs e)
    {
        BeginMoveDrag((PointerPressedEventArgs)e);
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