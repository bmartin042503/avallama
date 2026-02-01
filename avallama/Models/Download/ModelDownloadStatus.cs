// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using avallama.Constants;

namespace avallama.Models.Download;

public class ModelDownloadStatus
{
    // empty constructor for AXAML
    public ModelDownloadStatus() { }

    public ModelDownloadStatus(DownloadState state, string? message = null)
    {
        DownloadState = state;
        Message = message;
    }

    public DownloadState DownloadState { get; set; } = DownloadState.Downloadable;
    public string? Message { get; set; }
}
