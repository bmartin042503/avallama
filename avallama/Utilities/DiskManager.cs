using System;
using System.IO;

namespace avallama.Utilities;

/// <summary>
/// Utility class for managing disk related operations.
/// </summary>
public static class DiskManager
{
    // TODO: In the future when a custom storage location for models is implemented, this needs to be revisited.

    /// <summary>
    /// Checks if there is enough disk space available on the drive where the application data folder is located.
    /// This is useful for ensuring that there is sufficient space before performing operations such as downloading a model.
    /// It reserves a minimum of 10GB or 2% of the total disk size, whichever is larger, to prevent the disk from being completely filled.
    /// </summary>
    /// <param name="requiredBytes"> The amount of bytes an operation would need to be free on the disk </param>
    /// <returns> True if there is enough space (more than 10 GB or 2% of disk space + required bytes), false otherwise </returns>
    public static bool IsEnoughDiskSpaceAvailable(long requiredBytes)
    {
        const long minReserveBytes = 10L * 1024 * 1024 * 1024;
        const double reserveFraction = 0.02;

        var path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var rootDir = Directory.GetDirectoryRoot(path);
        var driveInfo = new DriveInfo(rootDir);
        try
        {
            if (driveInfo.IsReady)
            {
                var percentReserveBytes = (long)(driveInfo.TotalSize * reserveFraction);
                var reserveBytes = Math.Max(minReserveBytes, percentReserveBytes);
                return driveInfo.AvailableFreeSpace >= requiredBytes + reserveBytes;
            }
        }
        catch (IOException ex)
        {
            // TODO: InterruptService
        }

        return false;
    }
}
