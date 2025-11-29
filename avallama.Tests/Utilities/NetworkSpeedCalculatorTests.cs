// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Threading;
using System.Threading.Tasks;
using avallama.Utilities;
using Xunit;

namespace avallama.Tests.Utilities;

public class NetworkSpeedCalculatorTests
{
    // TODO: mock time duration (TimeProvider) instead of using Thread.Sleep or Task.Dleay
    /*
    [Fact]
    public async Task CalculateSpeed_WithSampleSizeOfTenMillionBytes_ReturnsSpeedAroundTenMegabytesPerSecond()
    {
        var networkSpeedCalculator = new NetworkSpeedCalculator();

        const int chunks = 5;
        const long sampleSize = 10_000_000;
        var downloadedBytes = 0L;
        var speed = 0.0;

        for (var i = 0; i < chunks; i++)
        {
            await Task.Delay(250); // 250 ms instead of waiting for a sec each time
            downloadedBytes += sampleSize;
            speed = networkSpeedCalculator.CalculateSpeed(downloadedBytes);
        }

        var roundedSpeed = Math.Round(speed / 4, 1); // we divide it by 4 so it calculates for 1 sec
        Assert.True(roundedSpeed is >= 7.5 and <= 12.5);
    }

    [Fact]
    public async Task CalculateSpeed_WithSampleSizeOfTenBillionBytes_ReturnsSpeedAroundThousandMegabytesPerSecond()
    {
        var networkSpeedCalculator = new NetworkSpeedCalculator();

        const int chunks = 10;
        const long sampleSize = 1_000_000_000;
        var downloadedBytes = 0L;
        var speed = 0.0;

        for (var i = 0; i < chunks; i++)
        {
            await Task.Delay(250); // 250 ms instead of waiting for a sec each time
            downloadedBytes += sampleSize;
            speed = networkSpeedCalculator.CalculateSpeed(downloadedBytes);
        }

        var roundedSpeed = Math.Round(speed / 4, 1); // we divide it by 4 so it calculates for 1 sec
        Assert.True(roundedSpeed is >= 750.0 and <= 1250.0);
    }*/
}
