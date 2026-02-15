// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

namespace avallama.Constants.States;

/// <summary>
/// Defines the lifecycle states of the local Ollama executable process.
/// </summary>
public enum OllamaProcessLifecycle
{
    /// <summary>
    /// The process is currently starting up.
    /// </summary>
    Starting,

    /// <summary>
    /// The process is actively running.
    /// </summary>
    Running,

    /// <summary>
    /// The process has been stopped or has not been started yet.
    /// </summary>
    Stopped,

    /// <summary>
    /// The process failed to start or exited unexpectedly.
    /// </summary>
    Failed,

    /// <summary>
    /// The Ollama executable was not found on the system.
    /// </summary>
    NotInstalled
}
