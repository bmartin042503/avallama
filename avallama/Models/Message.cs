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

public class FailedMessage : Message
{
    public FailedMessage() : base(LocalizationService.GetString("MESSAGE_GENERATION_FAILED"))
    {
        Id = -1;
    }
}
