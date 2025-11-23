// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Threading;
using avallama.Utilities;
using Xunit;

namespace avallama.Tests.Utilities;

public class NetworkSpeedCalculatorTests
{
    [Fact]
    public void CalculateSpeed_WithSampleSizeOfTenMillionBytes_ReturnsSpeedOfTenMegabytesPerSecond()
    {
        var networkSpeedCalculator = new NetworkSpeedCalculator();

        const int chunks = 5;
        const long sampleSize = 10_000_000;
        var downloadedBytes = 0L;
        var speed = 0.0;

        for (var i = 0; i < chunks; i++)
        {
            Thread.Sleep(250); // 250 ms instead of waiting for a sec each time
            downloadedBytes += sampleSize;
            speed = networkSpeedCalculator.CalculateSpeed(downloadedBytes);
        }

        var roundedSpeed = Math.Round(speed / 4, 1); // we divide it by 4 so it calculates for 1 sec
        Assert.True(roundedSpeed is >= 9.0 and <= 11.0);
    }

    [Fact]
    public void CalculateSpeed_WithSampleSizeOfTenBillionBytes_ReturnsSpeedOfThousandMegabytesPerSecond()
    {
        var networkSpeedCalculator = new NetworkSpeedCalculator();

        const int chunks = 10;
        const long sampleSize = 1_000_000_000;
        var downloadedBytes = 0L;
        var speed = 0.0;

        for (var i = 0; i < chunks; i++)
        {
            Thread.Sleep(250); // 250 ms instead of waiting for a sec each time
            downloadedBytes += sampleSize;
            speed = networkSpeedCalculator.CalculateSpeed(downloadedBytes);
        }

        var roundedSpeed = Math.Round(speed / 4, 1); // we divide it by 4 so it calculates for 1 sec
        Assert.True(roundedSpeed is >= 990.0 and <= 1100.0);
    }
}
