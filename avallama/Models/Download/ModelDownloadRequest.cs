// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using avallama.Utilities.Network;
using CommunityToolkit.Mvvm.ComponentModel;

namespace avallama.Models.Download;

public partial class ModelDownloadRequest : QueueItem
{
    public required string ModelName { get; init; }

    // TODO: modify NetworkSpeedCalculator to get the speed from instead of the property below
    public NetworkSpeedCalculator SpeedCalculator { get; } = new();
    public int DownloadPartCount { get; set; }

    [ObservableProperty] private long _downloadedBytes;
    [ObservableProperty] private long _totalBytes;
    [ObservableProperty] private double _downloadSpeed;
    [ObservableProperty] private ModelDownloadStatus? _status;
}
