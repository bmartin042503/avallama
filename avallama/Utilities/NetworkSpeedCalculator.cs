// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace avallama.Utilities
{

    /// <summary>
    /// Provides functionality to calculate the network download speed in megabytes per second (MB/s)
    /// over a short moving time window. This is useful for smoothing out fluctuations in instantaneous
    /// download speed measurements.
    /// </summary>
    public class NetworkSpeedCalculator
    {
        private readonly Stopwatch _stopwatch = new();
        private long _lastBytes;

        private readonly TimeSpan _timeWindow = TimeSpan.FromMilliseconds(500);
        private readonly Queue<(double Time, double Speed)> _samples = new();

        public NetworkSpeedCalculator()
        {
            _stopwatch.Start();
        }

        /// <summary>
        /// Calculates the current average download speed based on the number of completed bytes.
        /// </summary>
        /// <param name="completedBytes">The total number of bytes downloaded so far.</param>
        /// <returns>The average download speed in megabytes per second (MB/s) over the recent time window.</returns>
        /// <remarks>
        /// This method maintains a moving time window (default 500 milliseconds) and averages all
        /// speed samples within that window to smooth out fluctuations in network speed.
        /// On the first call, the method returns 0 as there is no prior data to calculate speed.
        /// </remarks>
        public double CalculateSpeed(long completedBytes)
        {
            var now = _stopwatch.Elapsed.TotalSeconds;

            if (_lastBytes == 0)
            {
                _lastBytes = completedBytes;
                return 0;
            }

            var downloaded = completedBytes - _lastBytes;
            var dt = now - _samples.LastOrDefault().Time;

            if (dt <= 0) dt = 0.000001;

            _lastBytes = completedBytes;

            var bytesPerSec = downloaded / dt;
            var mbPerSec = bytesPerSec / 1_000_000.0;

            _samples.Enqueue((now, mbPerSec));

            while (_samples.Count > 0 && now - _samples.Peek().Time > _timeWindow.TotalSeconds)
            {
                _samples.Dequeue();
            }

            return _samples.Average(s => s.Speed);
        }
    }
}
