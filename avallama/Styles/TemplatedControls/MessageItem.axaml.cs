// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System.Windows.Input;
using Avalonia;
using Avalonia.Controls.Primitives;

namespace avallama.Styles.TemplatedControls;

public class MessageItem : TemplatedControl
{
    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<MessageItem, string?>(nameof(Text));

    public static readonly StyledProperty<string?> SubTextProperty =
        AvaloniaProperty.Register<MessageItem, string?>(nameof(SubText));

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
}

