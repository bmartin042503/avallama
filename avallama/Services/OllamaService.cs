using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using avallama.Constants;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using avallama.Models;

namespace avallama.Services;

// TODO: ollama-llama-server process megfelelő kezelése (különben csemegézni fog a memóriából)
// TODO: 'Ollama' és 'ollama' process közti különbség (?) esetlegesen 'Ollama' ellenőrzése/bezárása

public class OllamaService
{
    private Process? _ollamaProcess;
    private Process? _ollamaLlamaServerProcess;
    private static readonly HttpClient HttpClient = new();
    private string OllamaPath { get; set; }

    // egy delegate ahol megadjuk hogy milyen metódus definícióval kell rendelkezniük a feliratkozó metódusoknak
    // ebben az esetben void visszatérésű ami ServiceStatus-t és string? típust vár
    public delegate void ServiceStatusChangedHandler(ServiceStatus status, string? message);

    // az event létrehozása, ami ugye az előzőleg létrehozott delegate típusú, tehát a megfelelő szignatúrájú metódusok
    // tudnak feliratkozni rá
    // a MainViewModelben az OllamaServiceStatusChanged iratkozik fel ide erre az eventre
    public event ServiceStatusChangedHandler? ServiceStatusChanged;

    public OllamaService()
    {
        OllamaPath = "";
    }

    private async Task Start()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
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
            OnServiceStatusChanged(
                ServiceStatus.Failed,
                LocalizationService.GetString("MACOS_NOT_SUPPORTED")
            );
            return;
        }

        var ollamaProcessCount = OllamaProcessCount();
        GetOllamaLlamaServerProcess();
        switch (ollamaProcessCount)
        {
            case 0: break;
            case 1:
                OnServiceStatusChanged(
                    ServiceStatus.Running,
                    LocalizationService.GetString("OLLAMA_ALREADY_RUNNING")
                );
                return;
            case >= 2:
                OnServiceStatusChanged(
                    ServiceStatus.Failed,
                    LocalizationService.GetString("MULTIPLE_INSTANCES_ERROR")
                );
                return;
        }

        if (_ollamaLlamaServerProcess != null)
        {
            KillProcess(_ollamaLlamaServerProcess);
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
            OnServiceStatusChanged(
                ServiceStatus.Failed,
                string.Format(LocalizationService.GetString("OLLAMA_FAILED"), ex.Message)
            );
            return;
        }

        // ha nem tud elindítani 'ollama' processt akkor null, tehát nincs telepítve
        // máshogy nem lehet null (szerintem)
        if (_ollamaProcess == null)
        {
            OnServiceStatusChanged(
                ServiceStatus.Failed,
                LocalizationService.GetString("OLLAMA_NOT_INSTALLED")
            );
            return;
        }

        var isServerRunning = await IsOllamaServerRunning();
        if (isServerRunning)
        {
            OnServiceStatusChanged(
                ServiceStatus.Running,
                LocalizationService.GetString("OLLAMA_STARTED")
            );
        }
        else
        {
            OnServiceStatusChanged(
                ServiceStatus.Failed,
                LocalizationService.GetString("SERVER_CONN_FAILED")
            );
        }
    }

    private static uint OllamaProcessCount()
    {
        var ollamaProcesses = Process.GetProcessesByName("ollama");
        return (uint)ollamaProcesses.Length;
    }

    private static async Task<bool> IsOllamaServerRunning()
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

    private void GetOllamaLlamaServerProcess()
    {
        var ollamaLlamaServerProcesses = Process.GetProcessesByName("ollama_llama_server");
        if (ollamaLlamaServerProcesses.Length != 1)
        {
            //TODO hibakezeles :sob:
            return;
        }

        _ollamaLlamaServerProcess = ollamaLlamaServerProcesses[0];
    }

    public void Stop()
    {
        GetOllamaLlamaServerProcess();
        KillProcess(_ollamaLlamaServerProcess);
        KillProcess(_ollamaProcess);
    }

    private static void KillProcess(Process? process)
    {
        if (process == null || process.HasExited) return;
        process.Kill();
        process.Dispose();
    }

    public async Task StartWithDelay(TimeSpan delay)
    {
        await Task.Delay(delay);
        await Start();
    }

    // meghívjuk az eventet, ami azt jelenti hogy a feliratkozott metódusok is meghívódnak
    // ebben az eseteben a MainViewModelben lévő hívódik meg és átadja neki az értékeket
    private void OnServiceStatusChanged(ServiceStatus status, string? message = null)
    {
        ServiceStatusChanged?.Invoke(status, message);
    }

    public async Task<bool> IsModelDownloaded()
    {
        const string url = "http://localhost:11434/api/tags";
        
        using var client = new HttpClient();
        var response = await client.GetAsync(url);
        var json = JsonNode.Parse(response.Content.ReadAsStringAsync().Result);
        return json?["models"]?.AsArray().Any(m => m?["name"]?.ToString() == "llama3.2:latest") ?? false;
    }

    public async IAsyncEnumerable<OllamaResponse> GenerateMessage(List<Message> messageHistory)
    {
        const string url = "http://localhost:11434/api/chat";

        ChatRequest chatRequest = new ChatRequest(messageHistory);
        string jsonPayload = chatRequest.ToJson();

        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
        
        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = content
        };

        using var client = new HttpClient();
        // TODO: itt jön egy exception egy már meglévő ollama processre hogy elutasította a kapcsolatot
        using var response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        await using var stream = await response.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream);

        string? line;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            if (!string.IsNullOrWhiteSpace(line))
            {
                OllamaResponse? json = null;
                try
                {
                    json = JsonSerializer.Deserialize<OllamaResponse>(line);
                }
                catch (JsonException)
                {
                    // insert error handling here
                }
                if (json != null)
                {
                    yield return json;
                }
            }
        }
    }
}