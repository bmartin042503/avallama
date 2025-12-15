// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using avallama.Tests.Fakes;
using avallama.Utilities;
using Xunit;

namespace avallama.Tests.Utilities;

public class NetworkSpeedCalculatorTests
{

    [Fact]
    public void CalculateSpeed_WithSampleSizeOfTenMillionBytes_ReturnsSpeedAroundTenMegabytesPerSecond()
    {
        var fakeTimer = new FakeTimeProvider();
        var networkSpeedCalculator = new NetworkSpeedCalculator(fakeTimer);

        const int chunks = 5;
        const long sampleSize = 10_000_000;
        var downloadedBytes = 0L;
        var speed = 0.0;

        for (var i = 0; i < chunks; i++)
        {
            fakeTimer.Advance(TimeSpan.FromSeconds(1));
            downloadedBytes += sampleSize;
            speed = networkSpeedCalculator.CalculateSpeed(downloadedBytes);
        }

        var roundedSpeed = Math.Round(speed, 1);
        Assert.True(roundedSpeed is >= 7.5 and <= 12.5);
    }

    [Fact]
    public void CalculateSpeed_WithSampleSizeOfTenBillionBytes_ReturnsSpeedAroundThousandMegabytesPerSecond()
    {
        var fakeTimer = new FakeTimeProvider();
        var networkSpeedCalculator = new NetworkSpeedCalculator(fakeTimer);

        const int chunks = 10;
        const long sampleSize = 1_000_000_000;
        var downloadedBytes = 0L;
        var speed = 0.0;

        for (var i = 0; i < chunks; i++)
        {
            fakeTimer.Advance(TimeSpan.FromSeconds(1));
            downloadedBytes += sampleSize;
            speed = networkSpeedCalculator.CalculateSpeed(downloadedBytes);
        }

        var roundedSpeed = Math.Round(speed, 1);
        Assert.True(roundedSpeed is >= 750.0 and <= 1250.0);
    }
}
