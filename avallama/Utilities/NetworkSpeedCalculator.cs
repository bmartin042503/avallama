// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
using System.Linq;

namespace avallama.Utilities
{
    public class NetworkSpeedCalculator
    {
        private long _lastCompleted;
        private DateTime _lastUpdateTime = DateTime.MinValue;
        private readonly Queue<double> _speedReadings = new();
        private readonly TimeSpan _timeWindow = TimeSpan.FromMilliseconds(100);

        public double CalculateSpeed(long completed)
        {
            var currentTime = DateTime.Now;

            if (_lastUpdateTime == DateTime.MinValue || completed == _lastCompleted)
            {
                _lastUpdateTime = currentTime;
                _lastCompleted = completed;
                return 0;
            }

            var downloadedBytes = completed - _lastCompleted;
            var elapsedTime = (currentTime - _lastUpdateTime).TotalSeconds;

            _lastUpdateTime = currentTime;
            _lastCompleted = completed;

            var speedInBytesPerSecond = downloadedBytes / elapsedTime;
            var speedInMbps = (speedInBytesPerSecond * 8) / 1_000_000;

            _speedReadings.Enqueue(speedInMbps);

            while (_speedReadings.Count > 0 && currentTime - _lastUpdateTime > _timeWindow)
            {
                _speedReadings.Dequeue();
            }
            return _speedReadings.Average();
        }
    }
}
