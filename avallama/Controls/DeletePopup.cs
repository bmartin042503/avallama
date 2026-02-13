// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Metadata;

namespace avallama.Controls;

public class DeletePopup : PopupFlyoutBase
{
    public static readonly StyledProperty<Control?> ItemsProperty =
        AvaloniaProperty.Register<DeletePopup, Control?>(nameof(Items));

    [Content]
    public Control? Items
    {
        get => GetValue(ItemsProperty);
        set => SetValue(ItemsProperty, value);
    }

    protected override Control CreatePresenter()
    {
        return new FlyoutPresenter
        {
            Background = null,
            BorderBrush = null,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(0),
            Content = Items
        };
    }
}
