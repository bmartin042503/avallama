// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using avallama.Utilities.Network;
using CommunityToolkit.Mvvm.ComponentModel;

namespace avallama.Models.Download;

/// <summary>
/// Represents a queue item specific to downloading an Ollama model.
/// </summary>
public partial class ModelDownloadRequest : QueueItem
{
    /// <summary>
    /// Gets the name of the model to be downloaded.
    /// </summary>
    public required string ModelName { get; init; }

    /// <summary>
    /// Gets the utility used to calculate the real-time download speed.
    /// </summary>
    public NetworkSpeedCalculator SpeedCalculator { get; } = new();

    /// <summary>
    /// Gets or sets the number of parts this download has been split into.
    /// </summary>
    public int DownloadPartCount { get; set; }

    /// <summary>
    /// The total number of bytes successfully downloaded so far.
    /// </summary>
    [ObservableProperty] private long _downloadedBytes;

    /// <summary>
    /// The total size of the model in bytes.
    /// </summary>
    [ObservableProperty] private long _totalBytes;

    /// <summary>
    /// The current download speed in megabytes per second.
    /// </summary>
    [ObservableProperty] private double _downloadSpeed;

    /// <summary>
    /// The current status of the download operation, including state and error messages.
    /// </summary>
    [ObservableProperty] private ModelDownloadStatus? _status;
}
