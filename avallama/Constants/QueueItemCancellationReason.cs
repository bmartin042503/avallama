// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

namespace avallama.Constants;

/// <summary>
/// Specifies the reasons why a <see cref="avallama.Models.QueueItem"/> might be canceled.
/// </summary>
public enum QueueItemCancellationReason
{
    /// <summary>
    /// The cancellation reason is unknown or not specified.
    /// </summary>
    Unknown,

    /// <summary>
    /// The item was canceled due to an explicit user request.
    /// </summary>
    UserCancelRequest,

    /// <summary>
    /// The item was canceled because the user paused the operation.
    /// </summary>
    UserPauseRequest,

    /// <summary>
    /// The item was canceled automatically by the system to scale down parallelism.
    /// </summary>
    SystemScaling
}
