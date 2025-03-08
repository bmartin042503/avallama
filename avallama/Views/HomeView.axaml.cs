using System;
using avallama.Services;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

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
    private Control? _sideBarControl = null;

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
            MainGrid.ColumnDefinitions = new ColumnDefinitions("Auto,*");
            _sideBarExpanded = false;
        }
        else
        {
            if (_sideBarControl == null) return;
            MainGrid.ColumnDefinitions = new ColumnDefinitions("3*,0.5*,7*");
            MainGrid.Children.Insert(0, _sideBarControl);
            _sideBarExpanded = true;
        }  
    }
}