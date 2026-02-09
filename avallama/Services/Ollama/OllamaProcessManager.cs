// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using avallama.Constants.States;
using avallama.Models.Ollama;

namespace avallama.Services.Ollama;

public delegate void OllamaProcessStatusChangedHandler(OllamaProcessStatus status);

public interface IOllamaProcessManager
{
    OllamaProcessStatus Status { get; }
    event OllamaProcessStatusChangedHandler? StatusChanged;
    Task StartAsync();
    Task StopAsync();
}

public class OllamaProcessManager : IOllamaProcessManager
{
    public Func<ProcessStartInfo, Process?> StartProcessFunc { get; init; } = Process.Start;
    public Func<int> GetProcessCountFunc { get; init; } = () => Process.GetProcessesByName("ollama").Length;
    private readonly SemaphoreSlim _startSemaphore = new(1, 1);
    private string OllamaPath { get; set; } = "";
    private Process? _ollamaProcess;

    public OllamaProcessStatus Status
    {
        get;
        private set
        {
            if (field == value) return;
            field = value;
            StatusChanged?.Invoke(value);
        }
    } = new(OllamaProcessState.Stopped);

    public event OllamaProcessStatusChangedHandler? StatusChanged;

    public async Task StartAsync()
    {
        await _startSemaphore.WaitAsync();

        if (Status.ProcessState == OllamaProcessState.Running) return;

        Status = new OllamaProcessStatus(OllamaProcessState.Starting);

        ConfigureOllamaPath();

        // Check for existing instances
        var ollamaProcessCount = GetProcessCountFunc();
        switch (ollamaProcessCount)
        {
            case 1:
                // If ollama is running as a systemd service under Linux, it should be counted here
                Status = new OllamaProcessStatus(OllamaProcessState.Running,
                    LocalizationService.GetString("OLLAMA_ALREADY_RUNNING"));
                return;
            case >= 2:
                // TODO: maybe change this to not set a failed state but rather keep one instance only and kill the other
                Status = new OllamaProcessStatus(OllamaProcessState.Failed,
                    LocalizationService.GetString("MULTIPLE_INSTANCES_ERROR"));
                return;
        }

        // Attempt to start process
        var startInfo = new ProcessStartInfo
        {
            FileName = OllamaPath,
            Arguments = "serve",
            UseShellExecute = false,
            CreateNoWindow = true
        };

        try
        {
            _ollamaProcess = StartProcessFunc(startInfo);
            if (_ollamaProcess != null)
            {
                Status = new OllamaProcessStatus(OllamaProcessState.Running);
                return;
            }

            Status = new OllamaProcessStatus(OllamaProcessState.NotInstalled);
        }
        catch (Win32Exception)
        {
            Status = new OllamaProcessStatus(OllamaProcessState.NotInstalled);
        }
        catch (Exception ex)
        {
            Status = new OllamaProcessStatus(OllamaProcessState.Failed,
                string.Format(LocalizationService.GetString("OLLAMA_FAILED"), ex.Message));
        }
        finally
        {
            _startSemaphore.Release();
        }
    }

    public async Task StopAsync()
    {
        await _startSemaphore.WaitAsync();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && IsOllamaRunningAsSystemdService())
        {
            // ollama is running as a systemd service, so it should not be killed
            return;
        }

        var ollamaProcessList = Process.GetProcessesByName("ollama");
        foreach (var process in ollamaProcessList)
        {
            if (process.HasExited) return;
            process.Kill();
            process.Dispose();
        }

        Status = new OllamaProcessStatus(OllamaProcessState.Stopped);
    }

    private void ConfigureOllamaPath()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            OllamaPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                @"Programs\Ollama\ollama");
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
                 RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            OllamaPath = @"/usr/local/bin/ollama";
        }
    }

    private static bool IsOllamaRunningAsSystemdService()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            throw new InvalidOperationException("Systemd check should only be called on Linux");
        }

        var psiRoot = new ProcessStartInfo
        {
            FileName = "systemctl",
            Arguments = "is-active ollama",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        var psiUser = new ProcessStartInfo
        {
            FileName = "systemctl",
            Arguments = "--user is-active ollama",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        try
        {
            using var processRoot = Process.Start(psiRoot);
            if (processRoot == null)
            {
                return false;
            }

            processRoot.WaitForExit();

            var outputRoot = processRoot.StandardOutput.ReadToEnd().Trim();
            if (outputRoot == "active")
            {
                return true;
            }

            // retry with user level check if root check failed
            using var processUser = Process.Start(psiUser);
            if (processUser == null)
            {
                return false;
            }

            processUser.WaitForExit();

            var outputUser = processUser.StandardOutput.ReadToEnd().Trim();
            return outputUser == "active";
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == 2)
        {
            // systemctl command failed because it does not exist, system does not use systemd
            return false;
        }
        catch (Win32Exception)
        {
            // other errors such as permission denied, should be handled in a different manner in the future
            return false;
        }
    }
}
