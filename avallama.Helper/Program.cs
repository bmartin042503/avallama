// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System.Diagnostics;

namespace avallama.Helper;

/// <summary>
///  Avallama helper process
/// </summary>
internal static class Program
{
    private static void Main(string[] args)
    {
        // avallama starts this process when it needs to restart the application
        // after starting it closes itself, then this process restarts avallama
        // later it can be used for updates as well

        // first argument: processId
        // second argument: processPath
        if (args.Length < 2) return;

        var processId = int.Parse(args[0]);
        var avallamaPath = args[1];

        try
        {
            var avallamaProcess = Process.GetProcessById(processId);

            // waits for avallama to close
            avallamaProcess.WaitForExit();

            // start avallama
            Process.Start(new ProcessStartInfo
            {
                FileName = avallamaPath,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }
}
