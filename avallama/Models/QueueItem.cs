// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System.Threading;
using CommunityToolkit.Mvvm.ComponentModel;

namespace avallama.Models;

public abstract class QueueItem : ObservableObject
{
    private readonly CancellationTokenSource _cts = new();
    public CancellationToken Token => _cts.Token;
    public void Cancel() => _cts.Cancel();
}
