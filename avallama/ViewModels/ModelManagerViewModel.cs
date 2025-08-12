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

    // ide lesz betöltve az összes model adat
    private IEnumerable<OllamaModel> _modelsData = [];

    public IEnumerable<SortingOption> SortingOptions { get; } = Enum.GetValues<SortingOption>();

    // megjelenített modellek, ez módosul, pl. keresésnél, rendezésnél stb.
    [ObservableProperty] private ObservableCollection<OllamaModel> _models = [];

    [ObservableProperty] private string _downloadedModelsInfo = string.Empty;
    [ObservableProperty] private bool _hasDownloadedModels;
    [ObservableProperty] private bool _hasModelsToDisplay;
    [ObservableProperty] private string _selectedModelName = string.Empty;
    [ObservableProperty] private OllamaModel? _selectedModel;

    private SortingOption _selectedSortingOption;

    public SortingOption SelectedSortingOption
    {
        get => _selectedSortingOption;
        set
        {
            _selectedSortingOption = value;
            SortModels();
            OnPropertyChanged();
        }
    }

    private string _searchBoxText = string.Empty;

    public string SearchBoxText
    {
        get => _searchBoxText;
        set
        {
            _searchBoxText = value;

            // ollama modellek újraszűrése a searchbox értéke alapján
            FilterModels();
            SortModels();

            OnPropertyChanged();
        }
    }

    public ModelManagerViewModel(DialogService dialogService)
    {
        DialogType = ApplicationDialog.ModelManager;

        _dialogService = dialogService;

        SelectedSortingOption = SortingOption.Downloaded;

        LoadModelsData();
        FilterModels();
        SortModels();

        if (!string.IsNullOrEmpty(SelectedModelName) && HasDownloadedModels)
        {
            var modelFromName = _modelsData.FirstOrDefault(m => m.Name == SelectedModelName);
            if (modelFromName != null) SelectedModel = modelFromName;
        }
    }

    private void LoadModelsData()
    {
        // TODO: kitörölni a Constants dir-ben lévő dummy servicet ha már össze van kötve egy rendes service-el
        _modelsData = DummyModelsService.GetDummyOllamaModels();

        var ollamaModels = _modelsData as OllamaModel[] ?? _modelsData.ToArray();
        if (ollamaModels.Length == 0) return;

        var downloadedModelsCount = ollamaModels.Count(m => m.DownloadStatus == ModelDownloadStatus.Downloaded);

        HasModelsToDisplay = true;
        HasDownloadedModels = downloadedModelsCount > 0;

        if (HasDownloadedModels)
        {
            var downloadedSizeSum = ollamaModels
                .Where(m => m.DownloadStatus == ModelDownloadStatus.Downloaded)
                .Sum(m => m.Size);

            var totalSizeInGb = downloadedSizeSum / 1_073_741_824.0;

            DownloadedModelsInfo =
                string.Format(
                    LocalizationService.GetString("DOWNLOADED_MODELS_INFO"),
                    downloadedModelsCount,
                    totalSizeInGb
                );
        }
    }

    // A rendezés beállítása alapján rendezi a modelleket
    private void SortModels()
    {
        var sortedModels = SelectedSortingOption switch
        {
            SortingOption.Downloaded => Models
                .Where(m => m.DownloadStatus == ModelDownloadStatus.Downloaded)
                .Concat(Models.Where(m => m.DownloadStatus != ModelDownloadStatus.Downloaded)),
            
            SortingOption.ParametersAscending => Models.OrderBy(m => m.Parameters),
            
            SortingOption.ParametersDescending => Models.OrderByDescending(m => m.Parameters),
            
            SortingOption.SizeAscending => Models.OrderBy(m => m.Size),
            
            SortingOption.SizeDescending => Models.OrderByDescending(m => m.Size),
            
            _ => Models
        };

        Models = new ObservableCollection<OllamaModel>(sortedModels);
    }


    // A keresés szövege alapján szűri a modelleket
    private void FilterModels()
    {
        if (!_modelsData.Any())
        {
            HasModelsToDisplay = false;
            return;
        }

        var search = SearchBoxText.Trim();
        var hasSearch = !string.IsNullOrEmpty(search);

        Models = new ObservableCollection<OllamaModel>(
            _modelsData
                .Where(m => !hasSearch || m.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
        );

        HasModelsToDisplay = Models.Count != 0;
        if (string.IsNullOrEmpty(SelectedModelName)) SelectedModelName = Models[0].Name;
    }

    // ez akkor hívódik meg ha a felhasználó a letöltés/törlésre kattint
    [RelayCommand]
    public void ModelAction(object parameter)
    {
        if (parameter is OllamaModel model)
        {
            // TODO: downloadstatus alapján letöltés/törlés/letöltés szüneteltetése
        }
    }

    [RelayCommand]
    public void SelectModel(object parameter)
    {
        if (parameter is string modelName)
        {
            if (string.IsNullOrEmpty(modelName)) return;
            SelectedModelName = modelName;

            var modelFromName = _modelsData.FirstOrDefault(m => m.Name == SelectedModelName);
            if (modelFromName != null) SelectedModel = modelFromName;
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