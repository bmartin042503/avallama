// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System.Linq;
using System.Runtime.InteropServices;
using avallama.Services;
using avallama.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace avallama.Views;

public partial class HomeView : UserControl
{
    public HomeView()
    {
        InitializeComponent();

        // focusable, so that if the messageblock has a selection, it can pass the focus to the homeview when clicking outside the messageblock
        // allowing the selection to be cleared
        Focusable = true;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            // set margin so the window control buttons have space on macOS
            SetMacOSMargin();
        }
        else
        {
            SideBarTopGrid.Margin = new Thickness(14,14,14,0);
            SideBarButton.Margin = new Thickness(0,-10,0,0);
        }

        // we handle pointerwheel scrolls globally, if not the scrollviewer would catch it
        // and if not handled separately, the scroll-to-bottom would appear when a new message is added as the scrollbar grows
        AddHandler(PointerWheelChangedEvent, OnGlobalPointerWheelChanged, RoutingStrategies.Tunnel);
    }

    private bool _sideBarExpanded = true;
    private double _sideBarWidth;
    private Control? _sideBarControl;
    private string _scrollSetting = string.Empty;
    private bool _userScrolledWithWheel;

    // checks what state the window and sidebar are in
    // and sets their margins accordingly so that on macOS the window control buttons are usable

    // ReSharper disable once InconsistentNaming
    private void SetMacOSMargin()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) return;

        // if the native macOS window is in full screen
        // this can't be reached through Avalonia, so a native external library was needed (see: MacOSInterop.cs)
        if (MacOSInterop.isKeyWindowInFullScreen())
        {
            SideBarTopGrid.Margin = new Thickness(14,14,14,0);
            SideBarButton.Margin = _sideBarExpanded ? new Thickness(0, -10, 0, 0) : new Thickness(10, -10, 0, 0);
        }
        else
        {
            if (_sideBarExpanded)
            {
                SideBarTopGrid.Margin = new Thickness(14, 30, 14, 0);
                SideBarButton.Margin = new Thickness(0, -10, 0, 0);
            }
            else
            {
                SideBarButton.Margin = new Thickness(10, 26, 0, 0);
            }
        }
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        SetMacOSMargin();
    }

    private void ScrollViewer_OnScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        if (_scrollSetting is "" or null)
        {
            if (DataContext is not HomeViewModel vm || vm.ScrollSetting == "")
            {
                _scrollSetting = "float";
            }
            else
            {
                _scrollSetting = vm.ScrollSetting;
            }
        }

        var scrollViewer = sender as ScrollViewer;
        if (_scrollSetting == "auto")
        {
            if (!(e.ExtentDelta.Y > 0)) return;
            scrollViewer?.ScrollToEnd();
        }
        else if (_scrollSetting == "float")
        {
            // scroll to bottom button appears when scrolling down
            if (e.OffsetDelta.Y > 10 && !ScrollToBottomBtn.IsVisible && _userScrolledWithWheel)
            {
                ScrollToBottomBtn.IsVisible = true;
                ScrollToBottomBtnShadow.IsVisible = true;
                ScrollToBottomBtnShadow.BoxShadow = new BoxShadows
                (
                    new BoxShadow
                    {
                        OffsetY = 3,
                        Blur = 20,
                        Color = new Color(120, 0, 0, 0),
                        Spread = 5
                    }
                );
            }
            // scroll up somewhat OR scroll down to the bottom AND user scrolled with wheel, so message generation didn't move the scrollbar
            else if (e.OffsetDelta.Y < 0 || scrollViewer?.Offset.Y + scrollViewer?.Viewport.Height >=
                     scrollViewer?.Extent.Height - 1
                     && _userScrolledWithWheel && ScrollToBottomBtn.IsVisible)
            {
                ScrollToBottomBtn.IsVisible = false;
                ScrollToBottomBtnShadow.IsVisible = false;
            }

            _userScrolledWithWheel = false;
        }
    }

    private void OnGlobalPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        _userScrolledWithWheel = true;
    }

    private void ScrollToBottomBtn_OnClick(object? sender, RoutedEventArgs e)
    {
        ConversationScrollViewer.ScrollToEnd();
        ScrollToBottomBtn.IsVisible = false;
        ScrollToBottomBtnShadow.IsVisible = false;
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
                // sidebar expand/hide button
                new ColumnDefinition(new GridLength(50, GridUnitType.Pixel)),
                // chat part
                new ColumnDefinition(new GridLength(7, GridUnitType.Star))
            };

            MainGrid.ColumnDefinitions = columnDefinitions;
            _sideBarExpanded = false;

            SideBarButton.Margin = new Thickness(10,-10,0,0);
        }
        else
        {
            if (_sideBarControl == null) return;
            var columnDefinitions = new ColumnDefinitions
            {
                // sidebar
                new ColumnDefinition(new GridLength(_sideBarWidth, GridUnitType.Pixel))
                    { MinWidth = 250, MaxWidth = 400 },
                // gridsplitter
                new ColumnDefinition(new GridLength(8, GridUnitType.Pixel)),
                // sidebar expand/hide button
                new ColumnDefinition(new GridLength(50, GridUnitType.Pixel)),
                // chat part
                new ColumnDefinition(new GridLength(7, GridUnitType.Star))
            };
            MainGrid.ColumnDefinitions = columnDefinitions;
            MainGrid.Children.Insert(0, _sideBarControl);
            _sideBarExpanded = true;
        }

        SetMacOSMargin();
    }

    private void SideBar_OnSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        // if the sidebar size is changed, then responsively set the new conversation button text
        _sideBarWidth = SideBar.Bounds.Width;

        // get textblock explicitly so we can change its visibility and the icon can be centered
        var buttonTextBlock = NewConversationBtn.GetTemplateChildren().FirstOrDefault(c => c is TextBlock) as TextBlock;
        switch (_sideBarWidth)
        {
            case < 300:
                if (buttonTextBlock != null) buttonTextBlock.IsVisible = false;
                NewConversationBtn.Content = string.Empty;
                break;
            case >= 300:
                if (buttonTextBlock != null) buttonTextBlock.IsVisible = true;
                NewConversationBtn.Content = LocalizationService.GetString("NEW");
                break;
        }
    }
}
