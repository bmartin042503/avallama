// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
using System.Linq;
using avallama.Utilities.Time;

namespace avallama.Utilities
{
    /// <summary>
    /// Provides functionality to calculate the network download speed in megabytes per second (MB/s)
    /// over a short moving time window. This is useful for smoothing out fluctuations in instantaneous
    /// download speed measurements.
    /// </summary>
    public class NetworkSpeedCalculator
    {
        private readonly ITimeProvider _timeProvider;
        private long _lastBytes;

        private readonly TimeSpan _timeWindow = TimeSpan.FromMilliseconds(500);
        private readonly Queue<(double Time, double Speed)> _samples = new();

        public NetworkSpeedCalculator(ITimeProvider? timeProvider = null)
        {
            // if it's null we initialize with a real stopwatch
            _timeProvider = timeProvider ?? new RealTimeProvider();
            _timeProvider.Start();
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
            var now = _timeProvider.Elapsed.TotalSeconds;

            if (_lastBytes == 0)
            {
                _lastBytes = completedBytes;
                return 0;
            }

            var downloaded = completedBytes - _lastBytes;
            var lastTime = _samples.Count > 0 ? _samples.Last().Time : 0;
            var dt = now - lastTime;

            if (dt <= 0.000001) dt = 0.000001;

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
