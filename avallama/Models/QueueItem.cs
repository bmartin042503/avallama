// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System.Threading;
using avallama.Constants;
using CommunityToolkit.Mvvm.ComponentModel;

namespace avallama.Models;

/// <summary>
/// Represents an abstract base class for items that can be enqueued and processed by the <see cref="avallama.Services.Queue.QueueService{T}"/>.
/// </summary>
public abstract class QueueItem : ObservableObject
{
    private CancellationTokenSource _cts = new();

    /// <summary>
    /// Gets the cancellation token associated with this specific queue item.
    /// </summary>
    public CancellationToken Token => _cts.Token;

    /// <summary>
    /// Gets or sets the reason why this item was canceled.
    /// Defaults to <see cref="QueueItemCancellationReason.Unknown"/>.
    /// </summary>
    public QueueItemCancellationReason QueueItemCancellationReason { get; set; } = QueueItemCancellationReason.Unknown;

    /// <summary>
    /// Cancels the execution of this specific queue item if cancellation has not already been requested.
    /// </summary>
    public void Cancel()
    {
        if (!_cts.IsCancellationRequested)
        {
            _cts.Cancel();
        }
    }

    /// <summary>
    /// Disposes the current cancellation token source and creates a new one,
    /// effectively resetting the cancellation state of the item so it can be retried or re-queued.
    /// </summary>
    public void ResetToken()
    {
        _cts.Dispose();
        _cts = new CancellationTokenSource();
    }
}
