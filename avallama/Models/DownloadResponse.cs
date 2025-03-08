// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System.Text.Json.Serialization;

namespace avallama.Models;

public class DownloadResponse
{
    [JsonPropertyName("status")]
    public string? Status { get; set; }
    [JsonPropertyName("digest")]
    public string? Digest { get; set; }
    [JsonPropertyName("total")]
    public long? Total { get; set; }
    [JsonPropertyName("completed")]
    public long? Completed { get; set; }
}