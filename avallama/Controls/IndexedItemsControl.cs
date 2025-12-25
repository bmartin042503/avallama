// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using Avalonia;
using Avalonia.Controls;

namespace avallama.Controls;

// ItemsControl in which the items have an index
// I created it, but it was not needed, however, it might be needed in the future
public class IndexedItemsControl : ItemsControl
{
    // attached property, every ContentPresenter element gets this
    // which can be reached as IndexedItemsControl.ItemIndex type
    public static readonly AttachedProperty<int> ItemIndexProperty =
        AvaloniaProperty.RegisterAttached<IndexedItemsControl, Control, int>(
            "ItemIndex");

    public static int GetItemIndex(Control control) =>
        control.GetValue(ItemIndexProperty);

    public static void SetItemIndex(Control control, int value) =>
        control.SetValue(ItemIndexProperty, value);

    protected override void PrepareContainerForItemOverride(Control container, object? item, int index)
    {
        base.PrepareContainerForItemOverride(container, item, index);

        if (GetItemIndex(container) == 0)
        {
            SetItemIndex(container, index);
        }
    }
}
