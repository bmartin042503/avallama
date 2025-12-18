// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace avallama.Dtos;

public class OllamaModelDetailsDto
{
    [JsonPropertyName("parent_model")]
    public string? ParentModel { get; set; }

    [JsonPropertyName("format")]
    public string? Format { get; set; }

    [JsonPropertyName("family")]
    public string? Family { get; set; }

    [JsonPropertyName("families")]
    public List<string>? Families { get; set; }

    [JsonPropertyName("parameter_size")]
    public string? ParameterSize { get; set; }

    [JsonPropertyName("quantization_level")]
    public string? QuantizationLevel { get; set; }
}
