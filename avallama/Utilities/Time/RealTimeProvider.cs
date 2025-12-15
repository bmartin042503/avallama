// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Diagnostics;

namespace avallama.Utilities.Time;

/// <summary>
/// A concrete implementation of <see cref="ITimeProvider"/> for production use.
/// It wraps the system's high-resolution <see cref="Stopwatch"/> to measure real-time execution.
/// </summary>
public class RealTimeProvider : ITimeProvider
{
    private readonly Stopwatch _stopwatch = new();

    /// <inheritdoc />
    public TimeSpan Elapsed => _stopwatch.Elapsed;

    /// <inheritdoc />
    public void Start() => _stopwatch.Start();
}
