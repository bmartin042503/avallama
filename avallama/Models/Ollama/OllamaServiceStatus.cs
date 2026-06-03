// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using avallama.Constants.States;

namespace avallama.Models.Ollama;

/// <summary>
/// Represents the unified status of Ollama (Process + API).
/// </summary>
public class OllamaServiceStatus(OllamaServiceState serviceState, string? message = null)
{
    /// <summary>
    /// Gets or sets the unified current state.
    /// </summary>
    public OllamaServiceState ServiceState { get; set; } = serviceState;

    /// <summary>
    /// Gets or sets an optional message regarding the status (e.g., error details).
    /// </summary>
    public string? Message { get; set; } = message;
}
