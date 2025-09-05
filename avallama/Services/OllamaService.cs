// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

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
using Avalonia.Threading;

namespace avallama.Services;

internal interface IOllamaService
{
    Task Start();
    void Stop();
    Task Retry();
    Task<bool> IsModelDownloaded();
    Task<string> GetModelParamNum(string modelName);
    IAsyncEnumerable<DownloadResponse> PullModel(string modelName);
    IAsyncEnumerable<OllamaResponse> GenerateMessage(List<Message> messageHistory);
}

public class OllamaService : IOllamaService
{
    public const int DefaultApiPort = 11434;
    public const string DefaultApiHost = "localhost";
    
    private Process? _ollamaProcess;
    public ServiceStatus? CurrentServiceStatus { get; private set; }
    public string? CurrentServiceMessage { get; private set; }
    private string OllamaPath { get; set; }
    private HttpClient _httpClient;

    // egy delegate ahol megadjuk hogy milyen metódus definícióval kell rendelkezniük a feliratkozó metódusoknak
    public delegate void ServiceStatusChangedHandler(ServiceStatus? status, string? message);

    // az event létrehozása, ami ugye az előzőleg létrehozott delegate típusú, tehát a megfelelő szignatúrájú metódusok
    // tudnak feliratkozni rá
    // a MainViewModelben az OllamaServiceStatusChanged iratkozik fel ide erre az eventre

    // ez most rinyál hogy jajj de jobb lenne a ServiceStatusChanged név a privátnak is, de muszáj hogy legyen privát
    // különben nem lennének kezelhetőek az add és remove accessorok
    public event ServiceStatusChangedHandler? ServiceStatusChanged;

    private string? _apiHost = DefaultApiHost;
    private string? _apiPort = DefaultApiPort.ToString();
    private readonly ConfigurationService _configurationService;
    private readonly DialogService _dialogService;
    
    // maximális időtartam ameddig a különböző kérésekre vár az alkalmazás
    // ez azért 15 mp, mert lassabb gépeknél több idő lehet mire legelső üzenet generálásnál megjön a kérés
    // ezt majd később lehet korrigálni kell
    private readonly TimeSpan _timeout = TimeSpan.FromSeconds(15); 

    private string ApiBaseUrl => $"http://{_apiHost}:{_apiPort}";

    public OllamaService(ConfigurationService configurationService, DialogService dialogService)
    {
        OllamaPath = "";
        _configurationService = configurationService;
        _dialogService = dialogService;
        _httpClient = new HttpClient();
        LoadSettings();
    }

    // Ezt a metódust minden API hívásnál meghívja az app hogy up-to-date beállításokkal rendelkezzen
    // anélkül hogy újra kelljen indítani a service beállítás módosításakor
    private void LoadSettings()
    {
        var hostSetting = _configurationService.ReadSetting(ConfigurationKey.ApiHost);
        _apiHost = string.IsNullOrEmpty(hostSetting) ? "localhost" : hostSetting;

        var portSetting = _configurationService.ReadSetting(ConfigurationKey.ApiPort);
        _apiPort = string.IsNullOrEmpty(portSetting) ? "11434" : portSetting;
        
        // BaseAddress beállítása, így az összes kérés alapértelmezetten efelé a cím felé fog menni
        var newBaseUri = new Uri($"http://{_apiHost}:{_apiPort}");
        if (_httpClient.BaseAddress == null || _httpClient.BaseAddress != newBaseUri)
        {
            _httpClient.Dispose();
            _httpClient = new HttpClient { BaseAddress = newBaseUri };
            _httpClient.Timeout = _timeout;
        }
    }

    public async Task Start()
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
            // mac-en és linuxon ugyanaz a path
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
                // TODO: ehelyett ténylegesen kezelje automatikusan a processeket és ne kelljen újraindítani manuálisan
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
        catch (Win32Exception)
        {
            // ez az az exception amikor nem tud a megadott pathre ollama processt indítani
            // ha nincs ollama telepítve akkor először ezt az exceptiont kapja el
            OnServiceStatusChanged(ServiceStatus.NotInstalled);
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
            OnServiceStatusChanged(ServiceStatus.NotInstalled);
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
            // újrapróbálkozik a kapcsolatra
            // macOS-en erre mindenképp szükség van, mert később indulhat el a szerver mint az ellenőrzés
            await Retry();
        }
    }

    private static uint OllamaProcessCount()
    {
        var ollamaProcesses = Process.GetProcessesByName("ollama");
        return (uint)ollamaProcesses.Length;
    }

    public async Task<bool> IsOllamaServerRunning()
    {
        LoadSettings();
        try
        {
            var response = await _httpClient.GetAsync("/api/version");
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
        _httpClient.Dispose();
    }
    
    public async Task Retry()
    {
        // Retrying event küldése (HomeViewModelnek, hogy megjelenítse a státuszt a UI-on)
        OnServiceStatusChanged(ServiceStatus.Retrying);

        var maxTotalWait = TimeSpan.FromSeconds(30); // max teljes idő, ameddig újrapróbálja
        var delay = TimeSpan.FromMilliseconds(200); // kezdő késleltetés
        var maxDelay = TimeSpan.FromSeconds(5); // max késleltetés
        var stopwatch = Stopwatch.StartNew();

        while (stopwatch.Elapsed < maxTotalWait)
        {
            if (await IsOllamaServerRunning())
            {
                OnServiceStatusChanged(
                    ServiceStatus.Running,
                    LocalizationService.GetString("OLLAMA_STARTED")
                );
                return;
            }

            await Task.Delay(delay);

            // növeli a késleltetést minden alkalommal, de úgy hogy ne menjen a max fölé
            delay = TimeSpan.FromMilliseconds(Math.Min(delay.TotalMilliseconds * 2, maxDelay.TotalMilliseconds));
        }

        OnServiceStatusChanged(
            ServiceStatus.Failed,
            LocalizationService.GetString("OLLAMA_CONNECTION_ERROR")
        );
    }

    private static void KillProcess(Process? process)
    {
        if (process == null || process.HasExited) return;
        process.Kill();
        process.Dispose();
    }

    public async Task<bool> IsModelDownloaded()
    {
        LoadSettings();
        
        try
        {
            var response = await _httpClient.GetAsync("/api/tags");
            var responseContent = await response.Content.ReadAsStringAsync();
            var json = JsonNode.Parse(responseContent);
            return json?["models"]?.AsArray().Any(m => m?["name"]?.ToString() == "llama3.2:latest" ||
                                                       m?["name"]?.ToString() == "llama3.2")
                   ?? false;
        }
        catch (JsonException ex)
        {
            _dialogService.ShowErrorDialog(ex.Message);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            OnServiceStatusChanged(
                ServiceStatus.Failed,
                LocalizationService.GetString("OLLAMA_CONNECTION_ERROR")
            );
        }
        return false;
    }

    public async Task<string> GetModelParamNum(string modelName)
    {
        LoadSettings();

        var payload = new
        {
            model = modelName
        };

        var jsonPayload = JsonSerializer.Serialize(payload);
        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        try
        {
            var response = await _httpClient.PostAsync("/api/show", content);
            var responseContent = await response.Content.ReadAsStringAsync();
            var json = JsonNode.Parse(responseContent);
            return (json?["details"]?["parameter_size"] == null ? "" : ":") +
                   json?["details"]?["parameter_size"]?.ToString().ToLower();
        }
        catch (JsonException ex)
        {
            _dialogService.ShowErrorDialog(ex.Message);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            OnServiceStatusChanged(
                ServiceStatus.Failed,
                LocalizationService.GetString("OLLAMA_CONNECTION_ERROR")
            );
        }

        return string.Empty;
    }

    public async IAsyncEnumerable<OllamaResponse> GenerateMessage(List<Message> messageHistory)
    {
        LoadSettings();

        var chatRequest = new ChatRequest(messageHistory);
        var jsonPayload = chatRequest.ToJson();

        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/chat")
        {
            Content = content
        };
        
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                // sikeres üzenetküldésnél szintén elküldi hogy fut az ollama
                // hogy ha esetleg megváltozna a host akkor annak megfelelően jelenítse meg a HomeViewModel
                OnServiceStatusChanged(ServiceStatus.Running);
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            OnServiceStatusChanged(
                ServiceStatus.Failed,
                LocalizationService.GetString("OLLAMA_CONNECTION_ERROR")
            );
            yield break;
        }

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
                catch (JsonException e)
                {
                    _dialogService.ShowErrorDialog(e.Message);
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
        LoadSettings();

        var payload = new
        {
            model = modelName,
            stream = true
        };

        var jsonPayload = JsonSerializer.Serialize(payload);

        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/pull")
        {
            Content = content
        };
        
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                // OK válasznál szintén elküldi hogy fut az ollama
                // hogy ha esetleg megváltozna a host akkor annak megfelelően jelenítse meg a HomeViewModel
                OnServiceStatusChanged(ServiceStatus.Running);
            }
            
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            OnServiceStatusChanged(
                ServiceStatus.Failed,
                LocalizationService.GetString("OLLAMA_CONNECTION_ERROR")
            );
            yield break;
        }

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
                catch (JsonException e)
                {
                    _dialogService.ShowErrorDialog(e.Message);
                }

                if (json != null)
                {
                    yield return json;
                }
            }
        }
    }

    // meghívjuk az eventet, ami azt jelenti hogy a feliratkozott metódusok is meghívódnak
    // erre egy metódus iratkozott fel, HomeViewModelben
    private void OnServiceStatusChanged(ServiceStatus? status, string? message = null)
    {
        CurrentServiceStatus = status;
        CurrentServiceMessage = message;

        // ezt azért adtam hozzá mert tesztelés során 'NSWindow should only be instantiated on the main thread!' kivételt dobott
        // macOS specific error i guess de azért legyen mindenképp UI szálon ha UI hívások vannak
        if (Dispatcher.UIThread.CheckAccess())
        {
            ServiceStatusChanged?.Invoke(status, message);
        }
        else
        {
            Dispatcher.UIThread.Post(() => { ServiceStatusChanged?.Invoke(status, message); });
        }
    }
}