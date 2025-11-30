// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using avallama.ViewModels;
using Avalonia;
using Avalonia.Controls;

namespace avallama.Views;

public partial class ScraperView : UserControl
{

    public ScraperView()
    {
        InitializeComponent();
    }

    protected override async void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        if (DataContext is ScraperViewModel scraperViewModel)
            await scraperViewModel.InitializeAsync();
    }
}

