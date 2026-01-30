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
using avallama.Services.Ollama;
using avallama.Services.Persistence;
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

// helper class for customizing application operations (start, stop)
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

    public async Task AskOllamaStart()
    {
        // show ollama start confirmation dialog
        var result = await _dialogService.ShowConfirmationDialog(
            title: LocalizationService.GetString("OLLAMA_RUN_FROM_DIALOG_TITLE"),
            positiveButtonText: LocalizationService.GetString("OLLAMA_RUN_FROM_DIALOG_LOCAL"),
            negativeButtonText: LocalizationService.GetString("OLLAMA_RUN_FROM_DIALOG_REMOTE"),
            description: LocalizationService.GetString("OLLAMA_RUN_FROM_DIALOG_DESC")
        );

        // if remote was selected
        if (result is ConfirmationResult { Confirmation: ConfirmationType.Negative })
        {
            // show input dialog with input fields specified
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

            // if an inputresult arrives, we extract the data from it
            if (dialogResult is InputResult inputResult)
            {
                var remoteServerInfo = inputResult.Results.ToList();
                _configurationService.SaveSetting(ConfigurationKey.ApiHost, remoteServerInfo[0]!);
                _configurationService.SaveSetting(ConfigurationKey.ApiPort, remoteServerInfo[1]!);
            }
        }
        // if local was selected
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
    // This is currently hard to test, what worked for me was to open avallama from the bin/Debug/net10.0 folder
    // then open the helper process with its arguments (passing it separately the avallama process id and the path)
    // we need to find an optimal way so that it can be easily used in dev stage but also publish both for prod
    public void Restart()
    {
        // start avallama helper process
        // this helper process waits until the application closes, then starts a completely new avallama
        var processPath = Environment.ProcessPath;
        var processId = Environment.ProcessId;

        // this currently searches for the process in the helper folder, so in dev this would be bin/Debug/net10.0/helper
        var helperPath = Path.Combine(AppContext.BaseDirectory, "helper", "avallama.helper");

        if (!File.Exists(helperPath))
        {
            throw new FileNotFoundException($"Helper executable not found at path: {helperPath}");
        }
        var attributes = File.GetAttributes(helperPath);
        if ((attributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint)
        {
            throw new InvalidOperationException($"Helper executable at path {helperPath} is a symbolic link, which is not allowed.");
        }

        var psi = new ProcessStartInfo
        {
            FileName = helperPath,
            Arguments = $"{processId} \"{processPath}\"",
            UseShellExecute = true,
            CreateNoWindow = false
        };

        // start the helper process
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
