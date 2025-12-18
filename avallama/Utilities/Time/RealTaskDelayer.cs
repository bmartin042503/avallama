// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace avallama.Utilities.Time;

/// <summary>
/// Abstraction for Task.Delay to enable deterministic unit testing.
/// </summary>
public interface ITaskDelayer
{
    Task Delay(TimeSpan delay, CancellationToken token = default);
}


public class RealTaskDelayer : ITaskDelayer
{
    public Task Delay(TimeSpan delay, CancellationToken token = default)
        => Task.Delay(delay, token);
}
