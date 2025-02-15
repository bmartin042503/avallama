using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using avallama.Constants;
using System.Text;
using System.Text.Json;
using avallama.Models;

namespace avallama.Services;

// TODO: ollama-llama-server process megfelelő kezelése (különben csemegézni fog a memóriából)
// TODO: 'Ollama' és 'ollama' process közti különbség (?) esetlegesen 'Ollama' ellenőrzése/bezárása

public class OllamaService
{
    private Process? _ollamaProcess;
    private Process? _ollamaLlamaServerProcess;
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
            case >=2:
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
    
    public async Task<GeneratedMessage?> GenerateMessage(string prompt)
    {
        const string url = "http://localhost:11434/api/generate";
        GeneratedMessage? generatedMessage = null;
        
        var data = new
        {
            model = "llama3.2",
            prompt,
            stream = false
        };

        using var client = new HttpClient();
        var jsonData = JsonSerializer.Serialize(data);
        var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

        try
        {
            var response = await client.PostAsync(url, content);

            if (response.IsSuccessStatusCode)
            {
                var responseString = await response.Content.ReadAsStringAsync();
                var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseString);

                if (jsonResponse.TryGetProperty("response", out var answer) && 
                    jsonResponse.TryGetProperty("eval_count", out var evalCount) &&
                    jsonResponse.TryGetProperty("eval_duration", out var evalDuration))
                {
                    generatedMessage = new GeneratedMessage(answer.GetString() ?? string.Empty, (double)evalCount.GetInt32()/evalDuration.GetInt64() * 1e9);
                }
            }
            else
            {
                generatedMessage = new GeneratedMessage("An error occured, please restart the application. Error message: " + response.StatusCode, 0);
            }
        }
        catch (Exception ex)
        {
            generatedMessage = new GeneratedMessage("Exception occured, please restart the application: " + ex.Message, 0);
        }

        return generatedMessage;
    }
}