// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using avallama.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace avallama.Models;

public class Message : ObservableObject
{
    public long Id { get; set; }

    public string Content
    {
        get;
        set => SetProperty(ref field, value);
    }

    public Message(string content)
    {
        Content = content;
    }
}

public class GeneratedMessage : Message
{
    public double GenerationSpeed
    {
        get;
        set => SetProperty(ref field, Math.Round(value, 2));
    }

    public GeneratedMessage(string content, double generationSpeed) : base(content)
    {
        GenerationSpeed = generationSpeed;
    }
}

// These message types are inevitable as UI needs to know what message to render and how
// must be excluded from saving into DB or sending as context to Ollama

public class FailedMessage : Message
{
    public FailedMessage(string content = "") : base(content)
    {
        Id = -1;
    }
}

public class TypingIndicatorMessage : Message
{
    public TypingIndicatorMessage() : base(string.Empty)
    {
        Id = -1;
    }
}


