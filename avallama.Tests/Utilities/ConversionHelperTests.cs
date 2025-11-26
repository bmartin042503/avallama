// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using avallama.Utilities;
using Xunit;

namespace avallama.Tests.Utilities;

public class ConversionHelperTests
{
    [Fact]
    public void BytesToReadableSize_ZeroBytes()
    {
        var result = ConversionHelper.BytesToReadableSize(0);
        Assert.Equal("0 B", result);
    }

    [Fact]
    public void BytesToReadableSize_OneGB()
    {
        var gb = 1000L * 1000 * 1000;
        var result = ConversionHelper.BytesToReadableSize(gb);
        Assert.Equal("1 GB", result);
    }

    [Fact]
    public void BytesToReadableSize_OnePointFiveGB()
    {
        var gb = 1000L * 1000 * 1000;
        var size = gb * 3 / 2;
        var result = ConversionHelper.BytesToReadableSize(size);
        Assert.Equal("1.5 GB", result);
    }
}

