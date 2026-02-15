// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using avallama.Constants.States;

namespace avallama.Models.Ollama;

/// <summary>
/// Represents the current status of the Ollama API connection, including state and an optional status message.
/// </summary>
/// <param name="connectionState">The current state of the API connection.</param>
/// <param name="message">An optional message providing details about the state (e.g., error messages).</param>
public class OllamaApiStatus(OllamaConnectionState connectionState, string? message = null)
{
    /// <summary>
    /// Gets or sets the current state of the API connection.
    /// </summary>
    public OllamaConnectionState ConnectionState { get; set; } = connectionState;

    /// <summary>
    /// Gets or sets an optional message regarding the status (e.g., error details).
    /// </summary>
    public string? Message { get; set; } = message;
}
