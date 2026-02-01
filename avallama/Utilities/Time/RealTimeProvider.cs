// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Diagnostics;

namespace avallama.Utilities.Time;

/// <summary>
/// Defines an abstraction for time measurement.
/// This interface allows decoupling time-dependent logic from the system clock,
/// enabling deterministic unit testing by injecting fake time providers.
/// </summary>
public interface ITimeProvider
{
    /// <summary>
    /// Gets the total elapsed time measured by the provider since it was started.
    /// </summary>
    TimeSpan Elapsed { get; }

    /// <summary>
    /// Starts, or resumes, measuring elapsed time.
    /// </summary>
    void Start();

    /// <summary>
    /// Stops measuring elapsed time.
    /// </summary>
    void Stop();
}

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

    /// <inheritdoc />
    public void Stop() => _stopwatch.Stop();
}
