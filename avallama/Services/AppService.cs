// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using avallama.ViewModels;
using avallama.Views;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace avallama.Services;

public interface IAppService
{
    Task CheckOllamaStart();
    void InitializeMainWindow();
    void Shutdown();
}

// segéd osztály az alkalmazással kapcsolatos műveletek (indítás, leállítás) személyre szabásához
public class ShutdownMessage() : ValueChangedMessage<bool>(true);

public class CheckOllamaStartMessage() : ValueChangedMessage<bool>(true);
public class AppService : IAppService
{
    private bool _isMainWindowInitialized;
    private readonly DialogService _dialogService;
    private readonly MainViewModel _mainViewModel;
    private readonly ConfigurationService _configurationService;

    public AppService(
        DialogService dialogService,
        MainViewModel mainViewModel,
        ConfigurationService configurationService,
        IMessenger messenger
    )
    {
        _dialogService = dialogService;
        _mainViewModel = mainViewModel;
        _configurationService = configurationService;
        messenger.Register<ShutdownMessage>(this, (r, msg) => { Shutdown(); });
        messenger.Register<CheckOllamaStartMessage>(this, (r, msg) =>
        {
            // TODO: ezt majd meg kell nézni, hogy mennyire optimális így, okozhat-e hibát
            
            // egyelőre fő szálon, mert apple NSWindow csak így megy, de ezt majd megnézem jobban még
            _ = CheckOllamaStart();
        });
    }

    public async Task CheckOllamaStart()
    {
        var result = await _dialogService.ShowConfirmationDialog(
            title: LocalizationService.GetString("OLLAMA_RUN_FROM_DIALOG_TITLE"),
            positiveButtonText: LocalizationService.GetString("OLLAMA_RUN_FROM_DIALOG_LOCAL"),
            negativeButtonText: LocalizationService.GetString("OLLAMA_RUN_FROM_DIALOG_REMOTE"),
            description: LocalizationService.GetString("OLLAMA_RUN_FROM_DIALOG_DESC")
        );
        if (result is ConfirmationResult confirmationResult)
        {
            if (confirmationResult.Confirmation == ConfirmationType.Negative)
            {
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
                            inputValue: 11434.ToString(),
                            validator: port => int.TryParse(port, out var parsed) && parsed is > 0 and < 65536,
                            validationErrorMessage: LocalizationService.GetString("INVALID_PORT_ERR")
                        )
                    }
                );
                if (dialogResult is InputResult inputResult)
                {
                    var remoteServerInfo = inputResult.Results.ToList();
                    _configurationService.SaveSetting(ConfigurationKey.ApiHost, remoteServerInfo[0]!);
                    if (remoteServerInfo[0]! == "localhost" || remoteServerInfo[0] == "127.0.0.1")
                    {
                        _configurationService.SaveSetting(ConfigurationKey.StartOllamaFrom, "local");
                    }
                    _configurationService.SaveSetting(ConfigurationKey.ApiPort, remoteServerInfo[1]!);
                    _configurationService.SaveSetting(ConfigurationKey.StartOllamaFrom, "remote");
                }
            }
            else 
            {
                _configurationService.SaveSetting(ConfigurationKey.StartOllamaFrom, "local");
            }
        }
    }

    public void Shutdown()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop) return;
        desktop.Shutdown();
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