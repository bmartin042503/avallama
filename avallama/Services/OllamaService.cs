using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using avallama.Constants;
using avallama.ViewModels;
using CommunityToolkit.Mvvm.Messaging;

namespace avallama.Services;

public class OllamaProcessInfo
{
    public ProcessStatus Status { get; }
    public string? Message { get; }

    public OllamaProcessInfo(ProcessStatus status, string? message = null)
    {
        Status = status;
        Message = message;
    }
}

public class OllamaService
{
    private readonly IMessenger _messenger;
    private Process? _ollamaProcess;

    public OllamaService(IMessenger messenger)
    {
        _messenger = messenger;
        _messenger.Register<ViewInteraction>(this, (recipient, viewInteraction) =>
        {
            if (viewInteraction.InteractionType == InteractionType.RestartProcess)
            {
                Start();
            } 
        });
    }

    private async void Start()
    {
        string ollamaPath = "";
        if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            ollamaPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            @"Programs\Ollama\ollama"
            );
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            ollamaPath = "/usr/local/bin/ollama";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            _messenger.Send(new OllamaProcessInfo(ProcessStatus.Failed, LocalizationService.GetString("MACOS_NOT_SUPPORTED")));
            return;
        }

        if (await IsOllamaRunning())
        {
            _messenger.Send(new OllamaProcessInfo(ProcessStatus.Running, LocalizationService.GetString("PROCESS_ALREADY_RUNNING")));
            return;
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = ollamaPath,
            Arguments = "serve",
            UseShellExecute = false,
            CreateNoWindow = true
        };

        try
        {
            _ollamaProcess = Process.Start(startInfo);
        }
        catch (Exception ex)
        {
            _messenger.Send(new OllamaProcessInfo(ProcessStatus.Failed, ex.Message));
        }

        if (_ollamaProcess != null)
        {
            _messenger.Send(new OllamaProcessInfo(ProcessStatus.Running));
        }
    }
    
    private static async Task<bool> IsOllamaRunning()
    {
        try
        {
            using var client = new HttpClient();
            var response = await client.GetAsync("http://localhost:11434/api/version");
            return response.StatusCode == HttpStatusCode.OK;
        }
        catch
        {
            return false;
        }
    }

    public void Stop()
    {
        if (_ollamaProcess != null && !_ollamaProcess.HasExited)
        {
            _ollamaProcess.Kill();
            _ollamaProcess.Dispose();
        }
    }

    public async Task StartWithDelay(TimeSpan delay)
    {
        await Task.Delay(delay);
        Start();
    }
}