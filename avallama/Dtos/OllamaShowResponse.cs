// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace avallama.Dtos;

public sealed class OllamaShowResponse
{
    public string? License { get; set; }

    [JsonPropertyName("model_info")]
    public Dictionary<string, JsonElement>? Model_Info { get; set; }
}
