using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using avallama.Constants;
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
    }

    public void Start()
    {
        // ez eddig csak Windows specifikus
        string ollamaPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            @"Programs\Ollama\ollama"
        );

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