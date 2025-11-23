// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;

namespace avallama.Dtos;

public class OllamaTagsResponse
{
    public List<OllamaTagsModel> Models { get; set; } = new();
}

// I don't know why these classes are "never instantiated" but it is used in deserialization
public class OllamaTagsModel
{
    public string? Name { get; set; }
    public string? Model { get; set; }
    // ReSharper disable once InconsistentNaming
    public DateTime? Modified_At { get; set; }
    public long? Size { get; set; }
    public string? Digest { get; set; }
    public OllamaModelDetails? Details { get; set; }
}

public class OllamaModelDetails
{
    // ReSharper disable once InconsistentNaming
    public string? Parent_Model { get; set; }
    public string? Format { get; set; }
    public string? Family { get; set; }
    public List<string>? Families { get; set; }
    // ReSharper disable once InconsistentNaming
    public string? Parameter_Size { get; set; }
    // ReSharper disable once InconsistentNaming
    public string? Quantization_Level { get; set; }
}
