// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;

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
}
