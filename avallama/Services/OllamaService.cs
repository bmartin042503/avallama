using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
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
    private string OllamaPath { get; set; }
    private uint OllamaProcesses { get; set; }

    public OllamaService(IMessenger messenger)
    {
        _messenger = messenger;
        OllamaPath = "";
    }

    private async void Start()
    {
        if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            OllamaPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            @"Programs\Ollama\ollama"
            );
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            OllamaPath = @"/usr/local/bin/ollama";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            // thank you tim apple
            _messenger.Send(new OllamaProcessInfo(ProcessStatus.Failed, LocalizationService.GetString("MACOS_NOT_SUPPORTED")));
            return;
        }

        CheckOllamaProcess();
        switch (OllamaProcesses)
        {
            case 0:
                break;
            case 1:
                _messenger.Send(new OllamaProcessInfo(ProcessStatus.Running, LocalizationService.GetString("PROCESS_ALREADY_RUNNING")));
                return;
            case >=2:
                _messenger.Send(new OllamaProcessInfo(ProcessStatus.Failed, LocalizationService.GetString("CLOSE_ALL_PROCESSES_ERROR")));
                return;
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = OllamaPath,
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

        if (_ollamaProcess == null)
        {
            //ez gyakorlatilag nem tud előfordulni szerintem, bármi processz kivétel már elkapódott volna, de azé itt hagyom
            _messenger.Send(new OllamaProcessInfo(ProcessStatus.Failed, LocalizationService.GetString("UNKNOWN_ERROR")));
        }
        
        bool isServerWorking = await TestOllamaConnection();
        if (isServerWorking)
        {
            _messenger.Send(new OllamaProcessInfo(ProcessStatus.Running));
        }
        else
        {
            _messenger.Send(new OllamaProcessInfo(ProcessStatus.Failed), LocalizationService.GetString("SERVER_CONN_FAILED"));
        }
    }
    
    private void CheckOllamaProcess()
    {
        Process[] ollamaProcesses = Process.GetProcessesByName("ollama");
        OllamaProcesses = (uint)ollamaProcesses.Length;
    }

    private static async Task<bool> TestOllamaConnection()
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