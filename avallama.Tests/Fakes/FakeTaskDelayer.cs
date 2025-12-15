// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Threading;
using System.Threading.Tasks;
using avallama.Utilities.Time;

namespace avallama.Tests.Fakes;

public class FakeTaskDelayer : ITaskDelayer
{
    private readonly FakeTimeProvider _timeProvider;

    public FakeTaskDelayer(FakeTimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    public Task Delay(TimeSpan delay, CancellationToken token = default)
    {
        _timeProvider.Advance(delay);
        return Task.CompletedTask;
    }
}
