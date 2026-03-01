// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

namespace avallama.Constants.States;

/// <summary>
/// Represents the various states a model download can be during its lifecycle.
/// </summary>
public enum DownloadState
{
    /// <summary>
    /// The model is available for download but has not been queued yet.
    /// </summary>
    Downloadable,

    /// <summary>
    /// The download request is waiting in the queue to be processed.
    /// </summary>
    Queued,

    /// <summary>
    /// The model is currently being downloaded.
    /// </summary>
    Downloading,

    /// <summary>
    /// The download process has been paused by the user.
    /// </summary>
    Paused,

    /// <summary>
    /// The model has been successfully downloaded and is ready for use.
    /// </summary>
    Downloaded,

    /// <summary>
    /// The download process encountered an error and failed.
    /// </summary>
    Failed
}
