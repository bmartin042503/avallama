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
    private double _sideBarWidth;
    private Control? _sideBarControl;

    private void ScrollViewer_OnScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        // Ha a görgetési terület függőlegesen növekszik (új üzenet elem) akkor legörget az aljára
        // ezt majd lecserélni a görgetési beállításra
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
            var columnDefinitions = new ColumnDefinitions
            {
                // sidebar
                new ColumnDefinition(new GridLength(0, GridUnitType.Pixel)),
                // gridsplitter
                new ColumnDefinition(new GridLength(0, GridUnitType.Pixel)),
                // sidebar expand/hide gomb
                new ColumnDefinition(new GridLength(50, GridUnitType.Pixel)),
                // chat rész
                new ColumnDefinition(new GridLength(7, GridUnitType.Star))
            };

            MainGrid.ColumnDefinitions = columnDefinitions;
            _sideBarExpanded = false;
        }
        else
        {
            if (_sideBarControl == null) return;
            var columnDefinitions = new ColumnDefinitions
            {
                // sidebar
                new ColumnDefinition(new GridLength(_sideBarWidth, GridUnitType.Pixel)) { MinWidth = 180, MaxWidth = 400 },
                // gridsplitter
                new ColumnDefinition(new GridLength(8, GridUnitType.Pixel)),
                // sidebar expand/hide gomb
                new ColumnDefinition(new GridLength(50, GridUnitType.Pixel)),
                // chat rész
                new ColumnDefinition(new GridLength(7, GridUnitType.Star))
            };
            MainGrid.ColumnDefinitions = columnDefinitions;
            MainGrid.Children.Insert(0, _sideBarControl);
            _sideBarExpanded = true;
        }  
    }

    private void SideBar_OnSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        // ha átméretezi a sidebart akkor beállítjuk reszponzívan az új csevegés gomb szövegét
        // mert ugye a konverter baszta átállítani
        _sideBarWidth = SideBar.Bounds.Width;
        var buttonText = _sideBarWidth switch
        {
            < 205 => string.Empty,
            >= 205 => LocalizationService.GetString("NEW"),
            // >= 375 => LocalizationService.GetString("NEW_CONVERSATION"),
            _ => string.Empty
        };
        NewConversationBtn.Content = buttonText;
    }
}