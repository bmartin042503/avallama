// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Threading.Tasks;
using avallama.Constants;
using avallama.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace avallama.ViewModels;

public partial class ScraperViewModel : PageViewModel
{
    private IOllamaService _ollamaService;
    private IDialogService _dialogService;

    private int _processedModels;

    [ObservableProperty] private string _progressText = string.Empty;

    public ScraperViewModel(IOllamaService ollamaService, IDialogService dialogService)
    {
        Page = ApplicationPage.Scraper;
        _ollamaService = ollamaService;
        _dialogService = dialogService;

        _ = ScrapeModels();
    }

    public async Task ScrapeModels()
    {
        try
        {
            await foreach (var model in _ollamaService.StreamAllScrapedModelsAsync())
            {
                Console.WriteLine($"Megjott a model: {model.Name}");
                _processedModels++;
                ProgressText = string.Format(LocalizationService.GetString("SCRAPER_MODELS_FOUND"), _processedModels);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error while loading the models data: {ex.Message}");
            // TODO: proper logging
        }
    }
}
