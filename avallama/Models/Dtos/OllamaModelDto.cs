// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System.Text.Json.Serialization;

namespace avallama.Models.Dtos;

public class OllamaModelDto
{
    [JsonPropertyName("name")] public string? Name { get; set; }

    [JsonPropertyName("size")] public long? Size { get; set; }

    [JsonPropertyName("details")] public OllamaModelDetailsDto? Details { get; set; }

    // [JsonPropertyName("model")]
    // public string? Model { get; set; }

    // [JsonPropertyName("modified_at")]
    // public DateTime? ModifiedAt { get; set; }

    // [JsonPropertyName("digest")]
    // public string? Digest { get; set; }
}
