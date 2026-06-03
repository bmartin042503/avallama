// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

namespace avallama.Constants.States;

/// <summary>
/// Represents the unified current state of the Ollama service.
/// </summary>
public enum OllamaServiceState
{
    /// <summary>
    /// The Ollama executable is not installed or could not be found on the system.
    /// </summary>
    NotInstalled,

    /// <summary>
    /// The Ollama service is currently stopped or not running.
    /// </summary>
    Stopped,

    /// <summary>
    /// The Ollama service is in the process of starting up or establishing a connection.
    /// </summary>
    Starting,

    /// <summary>
    /// The Ollama service is successfully running, connected, and ready to process requests.
    /// </summary>
    Ready,

    /// <summary>
    /// The Ollama service encountered an error, failed to start, or lost its connection.
    /// </summary>
    Failed
}
