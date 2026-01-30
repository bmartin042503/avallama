// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace avallama.Models.Dtos;

public class OllamaTagsResponse
{
    [JsonPropertyName("models")] public List<OllamaModelDto> Models { get; set; } = [];
}
