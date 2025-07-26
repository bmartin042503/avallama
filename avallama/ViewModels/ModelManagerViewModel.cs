// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
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

    private IEnumerable<OllamaModel>? _ollamaModelsData;
    
    [ObservableProperty] private ObservableCollection<OllamaModel> _downloadedModels = [];
    [ObservableProperty] private ObservableCollection<OllamaModel> _popularModels = [];
    [ObservableProperty] private bool _hasDownloadedModels;
    [ObservableProperty] private bool _hasPopularModels;
    [ObservableProperty] private bool _hasNoModelsToDisplay;
    [ObservableProperty] private string _downloadedModelsTitle = string.Empty;

    private string _searchBoxText = string.Empty;
    public string SearchBoxText
    {
        get => _searchBoxText;
        set
        {
            _searchBoxText = value;
            
            // ollama modellek újraszűrése a searchbox értéke alapján
            FilterModelsData();
            
            OnPropertyChanged();
        }
    }

    public ModelManagerViewModel(DialogService dialogService)
    {
        DialogType = ApplicationDialog.ModelManager;

        _dialogService = dialogService;

        LoadModelsData();
        FilterModelsData();
    }

    private void LoadModelsData()
    {
        // egyelőre dummy adatokkal, de itt lenne a lokális és popular modellek adatainak betöltése
        // ez csak azért megy pushban hogy látható legyen a működés és segítsen abban hogy kell majd használni
        var downloadingModel = new OllamaModel(
            "llama4",
            new Dictionary<string, string>
            {
                { "Parameters", "70B" },
                { "Quantization", "8 bits" }
            },
            new List<ModelLabel>
            {
                new("Insufficient VRAM", ModelLabelHighlight.Strong)
            },
            159323855360,
            ModelDownloadStatus.Downloading
        );
        downloadingModel.DownloadProgress += 20;
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
                3291225472,
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
                159323855360,
                ModelDownloadStatus.Downloading
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
                8569934592,
                ModelDownloadStatus.NoConnectionForDownload
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
                12884901888,
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
                17179869184,
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
                3261225472,
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
                    new ("10 tokens/sec"),
                    new ("Great for code"),
                    new ("Great for video generation")
                },
                3241225472,
                ModelDownloadStatus.NotEnoughSpaceForDownload
            ),
            downloadingModel
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
        
        _ollamaModelsData = dummyData;
        if (_ollamaModelsData.ToList().Count > 0)
        {
            HasNoModelsToDisplay = false;
        }
    }

    private void FilterModelsData()
    {
        if (_ollamaModelsData == null)
        {
            HasNoModelsToDisplay = true;
            return;
        }

        var search = SearchBoxText.Trim();
        var hasSearch = !string.IsNullOrEmpty(search);
        
        DownloadedModels = new ObservableCollection<OllamaModel>(
            _ollamaModelsData
                .Where(m => m.DownloadStatus == ModelDownloadStatus.Downloaded)
                .Where(m => !hasSearch || m.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
        );
        
        PopularModels = new ObservableCollection<OllamaModel>(
            _ollamaModelsData
                .Where(m => m.DownloadStatus != ModelDownloadStatus.Downloaded)
                .Where(m => !hasSearch || m.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
        );
        
        HasDownloadedModels = DownloadedModels.Count > 0;
        if (HasDownloadedModels)
        {
            DownloadedModelsTitle = string.Format(LocalizationService.GetString("DOWNLOADED_MODELS"),
                DownloadedModels.Count);
        }

        HasPopularModels = PopularModels.Count > 0;
        if (HasDownloadedModels || HasPopularModels)
        {
            HasNoModelsToDisplay = false;
        }
        else
        {
            HasNoModelsToDisplay = true;
        }
        
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