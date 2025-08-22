// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using avallama.Constants;
using avallama.ViewModels;
using avallama.Views;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Messaging;

namespace avallama.Services;

public interface IApplicationService
{
    Task AskOllamaStart();
    void InitializeMainWindow();
    void Shutdown();
    void Restart();
}

// segéd osztály az alkalmazással kapcsolatos műveletek (indítás, leállítás) személyre szabásához
public class ApplicationService : IApplicationService
{
    private bool _isMainWindowInitialized;
    private readonly DialogService _dialogService;
    private readonly MainViewModel _mainViewModel;
    private readonly ConfigurationService _configurationService;

    public ApplicationService(
        DialogService dialogService,
        MainViewModel mainViewModel,
        ConfigurationService configurationService,
        IMessenger messenger
    )
    {
        _dialogService = dialogService;
        _mainViewModel = mainViewModel;
        _configurationService = configurationService;
        messenger.Register<ApplicationMessage.Shutdown>(this, (_, _) => { Shutdown(); });
        messenger.Register<ApplicationMessage.Restart>(this, (_, _) => { Restart(); });
        
        messenger.Register<ApplicationMessage.AskOllamaStart>(this, (_, _) =>
        {
            Task.Run(async () =>
            {
                await Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    await AskOllamaStart();
                });
            });
        });
    }

    // TODO: ollama csatlakozásáról dialog, hogy sikerült-e stb.
    public async Task AskOllamaStart()
    {
        // ollama indítás confirmation dialog megjelenítése
        var result = await _dialogService.ShowConfirmationDialog(
            title: LocalizationService.GetString("OLLAMA_RUN_FROM_DIALOG_TITLE"),
            positiveButtonText: LocalizationService.GetString("OLLAMA_RUN_FROM_DIALOG_LOCAL"),
            negativeButtonText: LocalizationService.GetString("OLLAMA_RUN_FROM_DIALOG_REMOTE"),
            description: LocalizationService.GetString("OLLAMA_RUN_FROM_DIALOG_DESC")
        );
        
        // ha remote lett kiválasztva
        if (result is ConfirmationResult { Confirmation: ConfirmationType.Negative }) 
        {
            // input dialog megjelenítése inputfieldekkel megadva
            var dialogResult = await _dialogService.ShowInputDialog(
                title: LocalizationService.GetString("OLLAMA_REMOTE_DIALOG_TITLE"),
                description: LocalizationService.GetString("OLLAMA_REMOTE_DIALOG_DESC"), 
                inputFields: new List<InputField>
                {
                    new (
                        placeholder: LocalizationService.GetString("API_HOST_SETTING"),
                        validator: host => host == "localhost" || IPAddress.TryParse(host, out _),
                        validationErrorMessage: LocalizationService.GetString("INVALID_HOST_ERR")
                    ),
                    new (
                        placeholder: LocalizationService.GetString("API_PORT_SETTING"),
                        inputValue: OllamaService.DefaultApiPort.ToString(),
                        validator: port => int.TryParse(port, out var parsed) && parsed is > 0 and < 65536,
                        validationErrorMessage: LocalizationService.GetString("INVALID_PORT_ERR")
                    )
                }
            );
            
            // ha inputresult érkezik akkor kinyerjük belőle az adatokat
            if (dialogResult is InputResult inputResult)
            {
                var remoteServerInfo = inputResult.Results.ToList();
                _configurationService.SaveSetting(ConfigurationKey.ApiHost, remoteServerInfo[0]!);
                _configurationService.SaveSetting(ConfigurationKey.ApiPort, remoteServerInfo[1]!);
            }
        }
        // ha local lett kiválasztva
        else
        {
            _configurationService.SaveSetting(ConfigurationKey.ApiHost, "localhost");
            _configurationService.SaveSetting(ConfigurationKey.ApiPort, OllamaService.DefaultApiPort.ToString());
        }
    }

    public void Shutdown()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop) return;
        desktop.Shutdown();
    }

    // TODO:
    // ez egyelőre nehezen tesztelhető, ami nekem működött hogy megnyitottam az avallamát bin/Debug/net9.0 mappából
    // majd megnyitottam a helper processt az argumentumaival (átadtam neki külön-külön kikeresve az avallama process id-t, meg a path-et)
    // találni kell rá egy optimális módot hogy a dev stageben könnyen használható legyen de prod-ra is együtt publisholja a kettőt
    public void Restart()
    {
        // avallama helper process indítása
        // ez a helper process megvárja amíg az alkalmazás bezárul, majd elindít egy teljesen új avallamát
        var processPath = Environment.ProcessPath;
        var processId = Environment.ProcessId;
        
        var helperPath = Path.Combine(AppContext.BaseDirectory, "helper", "avallama.helper");
        
        var psi = new ProcessStartInfo
        {
            FileName = helperPath,
            Arguments = $"{processId} \"{processPath}\"",
            UseShellExecute = true,
            CreateNoWindow = false
        };

        // helper process indítása
        Process.Start(psi);
        Environment.Exit(0);
    }

    public void InitializeMainWindow()
    {
        if (_isMainWindowInitialized) return;
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop) return;
        desktop.MainWindow = new MainWindow
        {
            DataContext = _mainViewModel
        };
        desktop.MainWindow.Show();
        _isMainWindowInitialized = true;
    }
}