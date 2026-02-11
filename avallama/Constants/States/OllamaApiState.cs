// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

namespace avallama.Constants.States;

/// <summary>
/// Defines the possible connection states for the Ollama API client.
/// </summary>
public enum OllamaApiState
{
    /// <summary>
    /// The client is currently attempting to establish a connection.
    /// </summary>
    Connecting,

    /// <summary>
    /// The client has successfully connected to the Ollama API.
    /// </summary>
    Connected,

    /// <summary>
    /// The client is disconnected from the API.
    /// </summary>
    Disconnected,

    /// <summary>
    /// The client lost connection and is attempting to reconnect automatically.
    /// </summary>
    Reconnecting,

    /// <summary>
    /// The connection failed due to an error (e.g., timeout, unreachable host).
    /// </summary>
    Faulted
}
