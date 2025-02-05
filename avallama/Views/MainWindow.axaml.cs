using System;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace avallama.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        // Title bar eltüntetése (csak Windowson)
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
        var args = e as PointerPressedEventArgs;
        if (args == null) return;
        var positionY = args.GetPosition(this).Y;
        
        // ablakot csak Y:25 alatt lehet mozgatni, vagyis az ablak felső részén
        if(positionY < 25) BeginMoveDrag((PointerPressedEventArgs)e);
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