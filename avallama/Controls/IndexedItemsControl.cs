// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using Avalonia;
using Avalonia.Controls;

namespace avallama.Controls;

// ItemsControl aminek az elemei rendelkeznek index-el
// létrehoztam de mégsem kellett, viszont elképzelhető, hogy a jövőben kelleni fog
public class IndexedItemsControl : ItemsControl
{
    // attached property, ezt megkapja minden ContentPresenter elem
    // amit IndexedItemsControl.ItemIndex típusként lehet elérni
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
