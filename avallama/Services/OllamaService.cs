using System;
using System.Collections.Generic;
using System.ComponentModel;
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

// TODO: Letesztelni memóriakezelést (meg linuxra és macos-re megvalósítani xd)

public class OllamaService
{
    private Process? _ollamaProcess;
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
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            // mac-en is ugyanaz a path
            OllamaPath = @"/usr/local/bin/ollama";
        }

        var ollamaProcessCount = OllamaProcessCount();
        switch (ollamaProcessCount)
        {
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
        // ez az az exception amikor nem tud a megadott pathre ollama processt indítani
        // ha nincs ollama telepítve akkor először ezt az exceptiont kapja el
        catch (Win32Exception)
        {
            OnServiceStatusChanged(
                ServiceStatus.Failed,
                LocalizationService.GetString("OLLAMA_NOT_INSTALLED")
            );
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
            // 10 alkalommal késleltetéssel ellenőrzi a szerver elérhetőségét
            // ez azért kell mert macen pl. hamarabb ellenőrzi az ollama szervert mint hogy az elindulna
            // és emiatt instant errort ad vissza annak ellenére hogy később elindul a 11434-es porton a szerver

            const uint maxServerCheck = 10;
            uint serverCheck = 0;
            var serverAvailable = false;
            while (!serverAvailable && serverCheck != maxServerCheck)
            {
                serverAvailable = await IsOllamaServerRunning();
                serverCheck++;
                if (!serverAvailable)
                {
                    await Task.Delay(1250);
                    continue;
                }
                
                OnServiceStatusChanged(
                    ServiceStatus.Running,
                    LocalizationService.GetString("OLLAMA_STARTED")
                );
                return;
            }

            if (!serverAvailable)
            {
                OnServiceStatusChanged(
                    ServiceStatus.Failed,
                    LocalizationService.GetString("SERVER_CONN_FAILED")
                );
            }
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

    public void Stop()
    {
        var ollamaProcessList = Process.GetProcessesByName("ollama");
        foreach (var process in ollamaProcessList)
        {
            KillProcess(process);
        }
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
        return json?["models"]?.AsArray().Any(m => m?["name"]?.ToString() == "llama3.2:latest" || 
                                                            m?["name"]?.ToString() == "llama3.2") 
                                                            ?? false;
    }

    public async Task<string> GetModelParamNum(string modelName)
    {
        const string url = "http://localhost:11434/api/show";

        var payload = new
        {
            model = modelName
        };
        
        var jsonPayload = JsonSerializer.Serialize(payload);
        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
        
        using var client = new HttpClient();
        var response = await client.PostAsync(url, content);
        var json = JsonNode.Parse(response.Content.ReadAsStringAsync().Result);
        return ":" + json?["details"]?["parameter_size"]?.ToString().ToLower();
        
    }

    public async IAsyncEnumerable<OllamaResponse> GenerateMessage(List<Message> messageHistory)
    {
        const string url = "http://localhost:11434/api/chat";

        var chatRequest = new ChatRequest(messageHistory);
        var jsonPayload = chatRequest.ToJson();

        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
        
        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = content
        };

        using var client = new HttpClient();
        // TODO: itt jön egy exception egy már meglévő ollama processre hogy elutasította a kapcsolatot
        // és?
        /* ami azt jelenti hogy amikor egyszer elindítottam és generáltam üzenetet majd mégegyszer elindítottam
         * mindig jött az error a generálásnál hogy már van kapcsolat létesítve a 11434-esen, ez windowson volt
         * de valszeg ez a hiba akkor már eltűnt, szóval kitörölheted ezt idk xd
         */
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

    public async IAsyncEnumerable<DownloadResponse> PullModel(string modelName)
    {
        const string url = "http://localhost:11434/api/pull";

        var payload = new
        {
            model = modelName,
            stream = true
        };
        
        var jsonPayload = JsonSerializer.Serialize(payload);
        
        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
        
        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = content
        };
        
        using var client = new HttpClient();
        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        await using var stream = await response.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream);
        
        string? line;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            if (!string.IsNullOrWhiteSpace(line))
            {
                DownloadResponse? json = null;
                try
                {
                    json = JsonSerializer.Deserialize<DownloadResponse>(line);
                }
                catch (JsonException)
                {
                    // Console.WriteLine("error json no good serialize");
                }

                if (json != null)
                {
                    yield return json;
                }
            }
        }
    }
}