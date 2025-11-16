// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using avallama.Utilities;
using Xunit;

namespace avallama.Tests.Utilities;

public class ConversionHelperTests
{
    [Fact]
    public void GetSizeInGb_ZeroBytes()
    {
        var result = ConversionHelper.FormatSizeInGb(0);
        Assert.Equal("0 GB", result);
    }

    [Fact]
    public void GetSizeInGb_OneGiB()
    {
        long gib = 1000L * 1000 * 1000;
        var result = ConversionHelper.FormatSizeInGb(gib);
        Assert.Equal("1 GB", result);
    }

    [Fact]
    public void GetSizeInGb_OnePointFiveGiB()
    {
        long gib = 1000L * 1000 * 1000;
        long size = gib * 3 / 2; // 1.5 GiB
        var result = ConversionHelper.FormatSizeInGb(size);
        Assert.Equal("1.5 GB", result);
    }
}

