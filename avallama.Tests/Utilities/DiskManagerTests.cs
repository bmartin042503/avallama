// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.IO;
using avallama.Utilities;
using Xunit;

namespace avallama.Tests.Utilities;

public class DiskManagerTests
{
    [Fact]
    public void IsEnoughDiskSpaceAvailable_WhenRequiredBytesIsZero_ReturnsTrueIfReserveFitsOnThisMachine()
    {
        // Arrange (compute expected based on the same rules DiskManager uses)
        const long minReserveBytes = 10L * 1024 * 1024 * 1024;
        const double reserveFraction = 0.02;

        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var root = Directory.GetDirectoryRoot(appData);
        var drive = new DriveInfo(root);

        if (!drive.IsReady)
        {
            // DiskManager returns false when the drive isn't ready; avoid flakiness on weird environments.
            Assert.False(DiskManager.IsEnoughDiskSpaceAvailable(0));
            return;
        }

        var reserve = Math.Max(minReserveBytes, (long)(drive.TotalSize * reserveFraction));
        var expected = drive.AvailableFreeSpace >= reserve;

        var result = DiskManager.IsEnoughDiskSpaceAvailable(0);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void IsEnoughDiskSpaceAvailable_WhenRequiredBytesIsHuge_ReturnsFalse()
    {
        // Using a very large value makes this deterministic on any normal machine.
        // long.MaxValue / 2 equates to about 4.5 exabytes, which is way beyond any reasonable disk size.
        var result = DiskManager.IsEnoughDiskSpaceAvailable(long.MaxValue / 2);

        Assert.False(result);
    }
}
