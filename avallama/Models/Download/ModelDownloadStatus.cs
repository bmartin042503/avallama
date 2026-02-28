// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using avallama.Constants.States;

namespace avallama.Models.Download;

/// <summary>
/// Represents the current status and accompanying message of a model download operation.
/// </summary>
public class ModelDownloadStatus
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ModelDownloadStatus"/> class.
    /// Required empty constructor for AXAML instantiation.
    /// </summary>
    public ModelDownloadStatus() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ModelDownloadStatus"/> class with a specific state and an optional message.
    /// </summary>
    /// <param name="state">The current state of the download.</param>
    /// <param name="message">An optional localized message detailing the state (typically used for errors).</param>
    public ModelDownloadStatus(DownloadState state, string? message = null)
    {
        DownloadState = state;
        Message = message;
    }

    /// <summary>
    /// Gets or sets the current state of the download.
    /// </summary>
    public DownloadState DownloadState { get; set; } = DownloadState.Downloadable;

    /// <summary>
    /// Gets or sets the descriptive message associated with the current state.
    /// </summary>
    public string? Message { get; set; }
}
