// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System.Diagnostics;
using System.Runtime.InteropServices;
using avallama.Constants;
using CommunityToolkit.Mvvm.Input;

namespace avallama.ViewModels;

public partial class GreetingViewModel : PageViewModel
{
    private const string OllamaDownloadUrl = @"https://ollama.com/download/";
    public GreetingViewModel()
    {
        // beállítás, hogy a viewmodel milyen paget kezel
        Page = ApplicationPage.Greeting;
    }

    [RelayCommand]
    public void RedirectToOllamaDownload()
    {
        var processUrl = OllamaDownloadUrl;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            processUrl += "windows";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            processUrl += "linux";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            processUrl += "mac";
        }
        Process.Start(new ProcessStartInfo
        {
            FileName = processUrl,
            UseShellExecute = true
        });
    }
}