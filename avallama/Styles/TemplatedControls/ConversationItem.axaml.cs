// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Input;

namespace avallama.Styles.TemplatedControls;

public class ConversationItem : TemplatedControl
{
    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<MessageItem, string?>(nameof(Text));

    public static readonly StyledProperty<string?> SubTextProperty =
        AvaloniaProperty.Register<MessageItem, string?>(nameof(SubText));

    public static readonly DirectProperty<ConversationItem, Guid?> IdProperty =
        AvaloniaProperty.RegisterDirect<ConversationItem, Guid?>(
            nameof(Id),
            o => o.Id,
            (o, v) => o.Id = v,
            unsetValue: Guid.Empty
        );

    public static readonly DirectProperty<ConversationItem, Guid?> SelectedIdProperty =
        AvaloniaProperty.RegisterDirect<ConversationItem, Guid?>(
            nameof(SelectedId),
            o => o.SelectedId,
            (o, v) => o.SelectedId = v,
            unsetValue: Guid.Empty
        );

    public static readonly StyledProperty<ICommand?> SelectCommandProperty =
        AvaloniaProperty.Register<ConversationItem, ICommand?>(nameof(SelectCommand));

    public static readonly StyledProperty<ICommand?> DeleteCommandProperty =
        AvaloniaProperty.Register<ConversationItem, ICommand?>(nameof(DeleteCommand));

    public static readonly StyledProperty<object?> DeleteCommandParameterProperty =
        AvaloniaProperty.Register<ConversationItem, object?>(nameof(DeleteCommandParameter));

    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public string? SubText
    {
        get => GetValue(SubTextProperty);
        set => SetValue(SubTextProperty, value);
    }

    public Guid? Id
    {
        get;
        set => SetAndRaise(IdProperty, ref field, value);
    }

    public Guid? SelectedId
    {
        get;
        set => SetAndRaise(SelectedIdProperty, ref field, value);
    }

    public ICommand? SelectCommand
    {
        get => GetValue(SelectCommandProperty);
        set => SetValue(SelectCommandProperty, value);
    }

    public ICommand? DeleteCommand
    {
        get => GetValue(DeleteCommandProperty);
        set => SetValue(DeleteCommandProperty, value);
    }

    public object? DeleteCommandParameter
    {
        get => GetValue(DeleteCommandParameterProperty);
        set => SetValue(DeleteCommandParameterProperty, value);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        if (Id is null) return;

        if (SelectCommand is { } cmd && cmd.CanExecute(Id))
        {
            cmd.Execute(Id);
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        switch (change.Property.Name)
        {
            case nameof(SelectedId):
                if (SelectedId.HasValue && Id.HasValue)
                {
                    if (SelectedId.Value == Id.Value)
                    {
                        Classes.Add("selectedConversation");
                    }
                    else
                    {
                        Classes.Remove("selectedConversation");
                    }
                }
                break;
        }
    }
}

