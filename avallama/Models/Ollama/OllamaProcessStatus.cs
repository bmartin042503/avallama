// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using avallama.Constants.States;

namespace avallama.Models.Ollama;

/// <summary>
/// Represents the current status of the local Ollama process.
/// </summary>
/// <param name="processLifecycle">The current state of the process.</param>
/// <param name="message">An optional message providing details about the state (e.g. error messages).</param>
public class OllamaProcessStatus(OllamaProcessLifecycle processLifecycle, string? message = null)
{
    /// <summary>
    /// Gets or sets the current state of the local Ollama process.
    /// </summary>
    public OllamaProcessLifecycle ProcessLifecycle { get; set; } = processLifecycle;

    /// <summary>
    /// Gets or sets an optional message regarding the process status.
    /// </summary>
    public string? Message { get; set; } = message;
}
