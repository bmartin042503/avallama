// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using avallama.Constants.States;

namespace avallama.Models.Ollama;

public class OllamaProcessStatus(OllamaProcessState processState, string? message = null)
{
    public OllamaProcessState ProcessState { get; set; } = processState;
    public string? Message { get; set; } = message;
}
