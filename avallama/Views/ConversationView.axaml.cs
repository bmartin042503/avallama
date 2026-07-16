using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace avallama.Views;

public partial class ConversationView : UserControl
{
    public ConversationView()
    {
        InitializeComponent();
    }

    private void ScrollViewer_OnScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        throw new System.NotImplementedException();
    }
}

