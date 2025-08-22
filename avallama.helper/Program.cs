// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System.Diagnostics;

namespace avallama.helper;

/// <summary>
///  Avallama helper process
/// </summary>
internal static class Program
{
    private static void Main(string[] args)
    {
        // első argumentum: processId
        // második argumentum: processPath
        if (args.Length < 2) return;

        var processId = int.Parse(args[0]);
        var avallamaPath = args[1];
        
        try
        {
            // avallama process lekérdezése és a process bezárásának megvárása
            var avallamaProcess = Process.GetProcessById(processId);
            avallamaProcess.WaitForExit();

            // avallama indítása
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