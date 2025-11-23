// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using avallama.Constants;
using avallama.Models;
using avallama.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

namespace avallama.ViewModels;

public partial class ScraperViewModel : PageViewModel
{
    private readonly IOllamaService _ollamaService;
    private readonly IModelCacheService _modelCacheService;
    private readonly IDialogService _dialogService;
    private readonly IConfigurationService _configurationService;
    private readonly IMessenger _messenger;

    private CancellationTokenSource? _cancellationTokenSource;
    private CancellationToken _cancellationToken;

    private int _receivedModels;

    [ObservableProperty] private string _progressText = string.Empty;
    [ObservableProperty] private bool _isCancelEnabled = true;

    public ScraperViewModel(
        IOllamaService ollamaService,
        IModelCacheService modelCacheService,
        IDialogService dialogService,
        IConfigurationService configurationService,
        IMessenger messenger
    )
    {
        Page = ApplicationPage.Scraper;
        _ollamaService = ollamaService;
        _modelCacheService = modelCacheService;
        _dialogService = dialogService;
        _configurationService = configurationService;
        _messenger = messenger;
    }

    public async Task InitializeAsync()
    {
        await ScrapeModels();
    }

    private async Task ScrapeModels()
    {
        try
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _cancellationToken = _cancellationTokenSource.Token;

            var tmpModels = await _modelCacheService.GetCachedModelsAsync();
            if (tmpModels.Count == 0)
            {
                // cancel button disabled for first scraping since it's necessary for the app to work
                IsCancelEnabled = false;
            }

            var models = new List<OllamaModel>();
            await foreach (var model in _ollamaService.StreamAllScrapedModelsAsync(_cancellationToken))
            {
                _cancellationToken.ThrowIfCancellationRequested();

                _receivedModels++;
                ProgressText = string.Format(
                    LocalizationService.GetString("SCRAPER_MODELS_FOUND"),
                    _receivedModels
                );

                models.Add(model);
            }

            ProgressText = LocalizationService.GetString("SCRAPER_CACHING_MODELS");

            // This "calls" the scraper again, but the result is cached in OllamaService
            var families = await _ollamaService.GetScrapedFamiliesAsync();
            await _modelCacheService.CacheModelFamilyAsync(families);
            await _modelCacheService.CacheModelsAsync(models);

            _dialogService.ShowInfoDialog(LocalizationService.GetString("SCRAPING_FINISHED_DESC"));
            _configurationService.SaveSetting(ConfigurationKey.LastUpdatedCache, DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
            _messenger.Send(new ApplicationMessage.RequestPage(ApplicationPage.ModelManager));
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Scraping cancelled.");
            // TODO: proper logging
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error while loading the models data: {ex.Message}");
            // TODO: proper logging
        }
    }

    [RelayCommand]
    public void CancelScraping()
    {
        _cancellationTokenSource?.Cancel();
        _messenger.Send(new ApplicationMessage.RequestPage(ApplicationPage.Settings));
    }
}
