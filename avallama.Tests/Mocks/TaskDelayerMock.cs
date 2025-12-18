// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Threading;
using System.Threading.Tasks;
using avallama.Utilities.Time;

namespace avallama.Tests.Mocks;

public class TaskDelayerMock : ITaskDelayer
{
    private readonly TimeProviderMock _timeProviderMock;

    public TaskDelayerMock(TimeProviderMock timeProviderMock)
    {
        _timeProviderMock = timeProviderMock;
    }

    public Task Delay(TimeSpan delay, CancellationToken token = default)
    {
        _timeProviderMock.Advance(delay);
        return Task.CompletedTask;
    }
}
