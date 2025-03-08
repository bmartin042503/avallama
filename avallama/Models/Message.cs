// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using CommunityToolkit.Mvvm.ComponentModel;

namespace avallama.Models;

public class Message : ObservableObject
{
    private string _content = string.Empty;

    public string Content
    {
        get => _content;
        set => SetProperty(ref _content, value);
    }

    public Message(string content)
    {
        Content = content;
    }
}