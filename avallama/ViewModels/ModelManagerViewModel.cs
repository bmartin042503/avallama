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

    [ObservableProperty] private ObservableCollection<OllamaModel> _downloadedModelsData = [];
    [ObservableProperty] private ObservableCollection<OllamaModel> _popularModelsData = [];
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
        // ez csak azért megy pushban hogy látható legyen a működés és segítsen abban hogy kell majd használni
        var dummyData = new List<OllamaModel>
        {
            new(
                "llama3.2",
                new Dictionary<string, string>
                {
                    { "Parameters", "3.25B" },
                    { "Quantization", "4 bits" }
                },
                new List<ModelLabel> {
                    new ("Runs great"),
                    new ("43 tokens/sec")
                },
                3221225472,
                ModelDownloadStatus.Downloaded
            ),
            new(
                "llama4",
                new Dictionary<string, string>
                {
                    { "Parameters", "70B" },
                    { "Quantization", "8 bits" }
                },
                new List<ModelLabel> {
                    new ("Insufficient VRAM", ModelLabelHighlight.Strong)
                },
                150323855360,
                ModelDownloadStatus.ReadyForDownload
            ),
            new(
                "mistral7b",
                new Dictionary<string, string>
                {
                    { "Parameters", "7B" },
                    { "Quantization", "4 bits" }
                },
                new List<ModelLabel> {
                    new ("Fast inference"),
                    new ("Works on 8GB VRAM")
                },
                8589934592, // 8 GB
                ModelDownloadStatus.Downloaded
            ),
            new(
                "gemma2-9b",
                new Dictionary<string, string>
                {
                    { "Parameters", "9B" },
                    { "Quantization", "6 bits" }
                },
                new List<ModelLabel> {
                    new ("14 tokens/sec")
                },
                12884901888, // 12 GB
                ModelDownloadStatus.Downloaded
            ),
            new(
                "codellama-13b",
                new Dictionary<string, string>
                {
                    { "Parameters", "13B" }
                },
                new List<ModelLabel> {
                    new ("Good for code"),
                    new ("Insufficient VRAM", ModelLabelHighlight.Strong)
                },
                17179869184, // 16 GB
                ModelDownloadStatus.ReadyForDownload
            ),
            new(
                "phi-2",
                new Dictionary<string, string>
                {
                    { "Parameters", "2.7B" },
                    { "Quantization", "4 bits" }
                },
                new List<ModelLabel> {
                    new ("Lightweight"),
                    new ("Great for chatbots")
                },
                3221225472, // 3 GB
                ModelDownloadStatus.Downloaded
            ),
            new(
                "orca-mini",
                new Dictionary<string, string>
                {
                    { "Parameters", "3B" },
                    { "Quantization", "4 bits" }
                },
                new List<ModelLabel> {
                    new ("Tiny model"),
                    new ("10 tokens/sec")
                },
                3221225472, // 3 GB
                ModelDownloadStatus.NotEnoughSpaceForDownload
            )
        };

        // ez szerintem majd úgy működhetne hogy valami Service visszaadja az összes modelt
        // tehát azokat a modelleket amik le vannak töltve és láthatóak Ollama API-n keresztül meg egyelőre a beégetett popular modelleket
        // a serviceben meg úgy lenne esetleg megvalósítva hogy bejárva a beégetett popularmodelst létrehoz azokból OllamaModel elemeket
        // és megadja az OllamaModel elemeknek a beállításait, tehát pl. hogy a model mérete ráférne-e a tárhelyre, ha igen akkor mehet letölthetőként ReadyForDownloadként beállítva
        // ha nincs internetkapcsolat akkor pedig az összesnek NoConnectionForDownload
        // ez pedig itt különválogatná aszerint hogy Downloaded vagy valami más, és akkor azok elkülönítve jelennének meg
        
        // meg nyilván további infókat is hozzáadna az adott service, pl. labels, details, tehát a paraméterekről, kvantálásról stb. infók
        // a futtathatóságról, sebességről stb.
        // szerintem ez így működhet de ha van valami jobb ötlet írj
        DownloadedModelsData = new ObservableCollection<OllamaModel>(
            dummyData.Where(m => m.DownloadStatus == ModelDownloadStatus.Downloaded)
        );
        HasDownloadedModels = DownloadedModelsData.Count > 0;
        if (HasDownloadedModels)
        {
            DownloadedModelsTitle = string.Format(LocalizationService.GetString("DOWNLOADED_MODELS"),
                DownloadedModelsData.Count);
        }

        PopularModelsData = new ObservableCollection<OllamaModel>(
            dummyData.Where(m => m.DownloadStatus != ModelDownloadStatus.Downloaded)
        );
    }

    // ez akkor hívódik meg ha a felhasználó a letöltés/törlésre kattint
    // később esetleg kiterjeszthető a model beállításainak megjelenítésére (tehát törlés helyett beállítások ikon lenne és ott lehetne törölni is)
    [RelayCommand]
    public void ModelAction(object? parameter)
    {
        if (parameter is OllamaModel model)
        {
            // TODO: downloadstatus alapján letöltés/törlés/letöltés szüneteltetése
            // esetleg valami letöltő animáció elindítása, ilyesmik
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