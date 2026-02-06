// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using avallama.Constants.States;

namespace avallama.Models.Ollama;

public class OllamaApiStatus(OllamaApiState apiState, string? message = null)
{
    public OllamaApiState ApiState { get; set; } = apiState;
    public string? Message { get; set; } = message;
}
