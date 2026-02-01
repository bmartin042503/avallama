// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System.Threading;
using avallama.Constants;
using CommunityToolkit.Mvvm.ComponentModel;

namespace avallama.Models;

public abstract class QueueItem : ObservableObject
{
    private CancellationTokenSource _cts = new();
    public CancellationToken Token => _cts.Token;
    public CancellationReason CancellationReason { get; set; } = CancellationReason.Unknown;

    public void Cancel()
    {
        if (!_cts.IsCancellationRequested)
        {
            _cts.Cancel();
        }
    }

    public void ResetToken()
    {
        _cts.Dispose();
        _cts = new CancellationTokenSource();
    }
}
