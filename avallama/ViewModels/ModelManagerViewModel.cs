// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using avallama.Constants;
using avallama.Models;
using avallama.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace avallama.ViewModels;

public partial class ModelManagerViewModel : DialogViewModel
{
    private readonly DialogService _dialogService;
    private readonly OllamaService _ollamaService;

    [ObservableProperty] private ObservableCollection<OllamaModel> _loadedModelsData = [];
    [ObservableProperty] private bool _hasDownloadedModels;
    [ObservableProperty] private string _downloadedModelsTitle = string.Empty;

    public ModelManagerViewModel(DialogService dialogService, OllamaService ollamaService)
    {
        DialogType = ApplicationDialog.ModelManager;

        _dialogService = dialogService;
        _ollamaService = ollamaService;

        LoadModelsData();
    }
    private void LoadModelsData()
    {
        // egyelőre dummy adatokkal, de itt lenne a lokális és popular modellek adatainak betöltése
        var dummyData = new List<OllamaModel> {
            new(
                "llama3.2",
                3.2,
                4,
                3287638985,
                ModelDownloadStatus.Downloaded,
                ModelPerformanceStatus.RunsGreat,
                105.4
            ),
            new(
                "llama4",
                7,
                6,
                3287638985,
                ModelDownloadStatus.ReadyForDownload,
                ModelPerformanceStatus.Unknown,
                0.0
            ),
            new(
                "llama5",
                30,
                0,
                30287638985,
                ModelDownloadStatus.ReadyForDownload,
                ModelPerformanceStatus.InsufficientVram,
                0.0
            ),
            new(
                "llama6",
                60,
                4,
                26287638985,
                ModelDownloadStatus.ReadyForDownload,
                ModelPerformanceStatus.RunsOkay,
                0.0
            )
        };
        
        LoadedModelsData = new ObservableCollection<OllamaModel>(dummyData);
        var loadedModelsCount = LoadedModelsData.Count(model => model.DownloadStatus == ModelDownloadStatus.Downloaded);
        HasDownloadedModels = loadedModelsCount > 0;
        if (HasDownloadedModels)
        {
            DownloadedModelsTitle = string.Format(LocalizationService.GetString("DOWNLOADED_MODELS"), loadedModelsCount);
        }
    }

    [RelayCommand]
    public void Close()
    {
        _dialogService.CloseDialog(ApplicationDialog.ModelManager);
    }

    [RelayCommand]
    public void ShowInfo()
    {
        _dialogService.ShowInfoDialog("ModelManager info here");
    }
}