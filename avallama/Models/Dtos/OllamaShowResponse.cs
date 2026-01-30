// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace avallama.Models.Dtos;

public sealed class OllamaShowResponse
{
    [JsonPropertyName("license")] public string? License { get; set; }

    [JsonPropertyName("model_info")] public Dictionary<string, JsonElement>? ModelInfo { get; set; }
}
