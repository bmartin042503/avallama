// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using avallama.Services;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace avallama.Views;

public partial class HomeView : UserControl
{
    public HomeView()
    {
        InitializeComponent();
        
        // focusable, hogy ha a messageblockban van kijelölés akkor átadhassa a homeviewnak a fókuszt ha kikattintanak a messageblockból
        // és így a kijelölés törölhető
        Focusable = true;
    }

    private bool _sideBarExpanded = true;
    private Control? _sideBarControl;

    private void ScrollViewer_OnScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        // Ha a görgetési terület függőlegesen növekszik (új üzenet elem) akkor legörget az aljára
        if (e.ExtentDelta.Y > 0)
        {
            var scrollViewer = sender as ScrollViewer;
            if (scrollViewer == null) return;
            scrollViewer.ScrollToEnd();
        }
    }

    private void SideBarBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_sideBarExpanded)
        {
            _sideBarControl = SideBar;
            MainGrid.Children.RemoveAt(0);
            MainGrid.ColumnDefinitions = new ColumnDefinitions("0,0,50,7*");
            _sideBarExpanded = false;
        }
        else
        {
            if (_sideBarControl == null) return;
            MainGrid.ColumnDefinitions = new ColumnDefinitions("300,8,50,7*");
            MainGrid.Children.Insert(0, _sideBarControl);
            _sideBarExpanded = true;
        }  
    }

    private void SideBar_OnSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        // ha átméretezi a sidebart akkor beállítjuk reszponzívan az új csevegés gomb szövegét
        // mert ugye a konverter baszta átállítani
        var sideBarWidth = SideBar.Bounds.Width;
        var buttonText = sideBarWidth switch
        {
            < 205 => string.Empty,
            < 375 => LocalizationService.GetString("NEW"),
            >= 375 => LocalizationService.GetString("NEW_CONVERSATION"),
            _ => string.Empty
        };
        NewConversationBtn.Content = buttonText;
    }
}