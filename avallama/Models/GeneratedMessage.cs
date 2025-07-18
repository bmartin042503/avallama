﻿// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;

namespace avallama.Models;

public class GeneratedMessage : Message
{
    private double _generationSpeed;
    public double GenerationSpeed
    {
        get => _generationSpeed;
        set => SetProperty(ref _generationSpeed, Math.Round(value, 2));
    }

    public GeneratedMessage(string content, double generationSpeed) : base(content)
    {
        GenerationSpeed = generationSpeed;
    }
}