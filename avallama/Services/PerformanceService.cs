using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace avallama.Services;

public class PerformanceService
{
    private OSPlatform Platform { get; set; }

    public PerformanceService()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Platform = OSPlatform.Windows;
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            Platform = OSPlatform.Linux;
        }
        else
        {
            // stay hungry stay foolish
            throw new PlatformNotSupportedException("Platform not supported");
        }
    }

    public async Task<double> CalculateCpuUsage()
    {
        if (Platform == OSPlatform.Windows)
        {
            return await CalculateCpuUsageWindows();
        }

        if (Platform == OSPlatform.Linux)
        {
            return await CalculateCpuUsageLinux();
        }

        return 0;
    }

    private async Task<double> CalculateCpuUsageWindows()
    {
        // enélkül sír az interpreter
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return 0;

        using (var cpuCounter = new PerformanceCounter("Processor Information", "% Processor Utility", "_Total"))
        {
            cpuCounter.NextValue();
            await Task.Delay(1000);
            return Math.Round(cpuCounter.NextValue());
        }
    }

    private async Task<double> CalculateCpuUsageLinux()
    {
        var firstSample = ReadCpuUsage();
        await Task.Delay(1000);
        var secondSample = ReadCpuUsage();

        var totalDiff = (secondSample.Total - firstSample.Total);
        var idleDiff = (secondSample.Idle - firstSample.Idle);

        if (totalDiff == 0) return 0;

        var cpuUsage = (1.0 - (idleDiff / totalDiff)) * 100.0;
        return cpuUsage;
    }

    private (double Total, double Idle) ReadCpuUsage()
    {
        // ngl nemtom ez mi a fasz
        var cpuLine = File.ReadLines("/proc/stat").FirstOrDefault(line => line.StartsWith("cpu "));
        if (cpuLine == null) return (0, 0);

        var parts = cpuLine.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
            .Skip(1)
            .Select(double.Parse)
            .ToArray();

        double idleTime = parts[3];
        double totalTime = parts.Sum();

        return (totalTime, idleTime);
    }
    
    public double CalculateMemoryUsage()
    {
        if (Platform == OSPlatform.Windows)
        {
            return CalculateMemoryUsageWindows();
        }

        if (Platform == OSPlatform.Linux)
        {
            return CalculateMemoryUsageLinux();
        }

        return 0;
    }

    private double CalculateMemoryUsageWindows()
    {
        // "This call site is reachable on all platforms, PerformanceCounter is only available on Windows" waaaaaaaa
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return 0;
        
        using (var performanceCounter = new PerformanceCounter("Memory", "% Committed Bytes In Use"))
        {
            return Math.Round(performanceCounter.NextValue());
        }
    }

    private double CalculateMemoryUsageLinux()
    {
        string[] lines = File.ReadAllLines("/proc/meminfo");

        float totalMemory = 0, freeMemory = 0, buffers = 0, cached = 0;

        foreach (string line in lines)
        {
            if (line.StartsWith("MemTotal:"))
                totalMemory = ParseLine(line);
            else if (line.StartsWith("MemFree:"))
                freeMemory = ParseLine(line);
            else if (line.StartsWith("Buffers:"))
                buffers = ParseLine(line);
            else if (line.StartsWith("Cached:"))
                cached = ParseLine(line);
        }

        float usedMemory = totalMemory - (freeMemory + buffers + cached);
        return (usedMemory / totalMemory) * 100;
    }


    private static float ParseLine(string line)
    {
        var parts = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        return float.Parse(parts[1]) / 1024;
    }
}