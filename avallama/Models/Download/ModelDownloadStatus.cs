// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using avallama.Constants;

namespace avallama.Models.Download;

public record ModelDownloadStatus(DownloadState DownloadState = DownloadState.Downloadable, string? Message = null);
