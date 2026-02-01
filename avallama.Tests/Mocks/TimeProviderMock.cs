// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using avallama.Utilities.Time;

namespace avallama.Tests.Mocks;

public class TimeProviderMock : ITimeProvider
{
    public TimeSpan Elapsed { get; private set; } = TimeSpan.Zero;
    public void Start() { /* it's a fake so it starts nothing */ }
    public void Stop() { /* it's a fake so it stops nothing */ }
    public void Advance(TimeSpan amount) => Elapsed += amount;
}
