// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Threading.Tasks;
using avallama.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;

namespace avallama.Views;

public partial class ScraperView : UserControl
{
    private readonly DispatcherTimer? _timer;

    public ScraperView()
    {
        InitializeComponent();

        if (Spinner.RenderTransform is not RotateTransform rotateTransform) return;

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(20)
        };
        _timer.Tick += (_, _) =>
        {
            rotateTransform.Angle = (rotateTransform.Angle + 5) % 360;
        };
    }

    protected override async void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _timer?.Start();

        if (DataContext is ScraperViewModel scraperViewModel)
            await scraperViewModel.InitializeAsync();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _timer?.Stop();
    }

}

