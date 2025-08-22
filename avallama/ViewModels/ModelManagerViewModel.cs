// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using avallama.Constants;
using avallama.Models;
using avallama.Services;
using avallama.Utilities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace avallama.ViewModels;

// TODO: összekötni valós adatokkal, valós letöltés implementációval stb.
public partial class ModelManagerViewModel : PageViewModel
{
    private readonly DialogService _dialogService;

    // ide lesz betöltve az összes model adat
    private IEnumerable<OllamaModel> _modelsData = [];

    // modellrendezési opciók
    public IEnumerable<SortingOption> SortingOptions { get; } = Enum.GetValues<SortingOption>();

    // megjelenített modellek, ez módosul, pl. keresésnél, rendezésnél stb.
    [ObservableProperty] private ObservableCollection<OllamaModel> _models = [];

    [ObservableProperty] private string _downloadedModelsInfo = string.Empty;
    [ObservableProperty] private bool _hasDownloadedModels;
    [ObservableProperty] private bool _hasModelsToDisplay;
    [ObservableProperty] private string _selectedModelName = string.Empty;
    [ObservableProperty] private OllamaModel? _selectedModel;
    [ObservableProperty] private bool _isModelInfoBlockVisible;

    // a jelenleg letöltés alatt álló modell letöltési sebessége Mbps-ben
    [ObservableProperty] private double _downloadSpeed;

    [ObservableProperty] private long _downloadedBytes;

    private SortingOption _selectedSortingOption;

    // ez tudja észlelni ha egy task cancellelve van, kell egy tokenSource és annak a tokenjét beállítani a taskra
    // aztán kivételt dobva fogja megszakítani a taskot
    private CancellationTokenSource _downloadCancellationTokenSource = new();

    // a jelenleg letöltés alatt álló modell
    private OllamaModel? _downloadingModel;

    // TODO: kisebb hiba kijavítása
    // valamiért a ParametersDescending értéket állítja be a Downloaded után, aztán visszaállítja Downloaded-re
    // fogalmam sincs hogy mi állítja át arra egy pillanatra de emiatt nem lehet a legelső Modelsben lévő elemet kiválasztani, hogy
    // indításnál a legelső elem legyen felül és ez legyen kiválasztva
    
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
        Page = ApplicationPage.ModelManager;
        _dialogService = dialogService;
        
        LoadModelsData();
        
        // SelectedSortingOption = SortingOption.Downloaded;

        if ((!HasModelsToDisplay || !string.IsNullOrEmpty(SelectedModelName)) && SelectedModel != null) return;
        SelectedModelName = Models[0].Name;
        SelectedModel = Models[0];
    }

    private void LoadModelsData()
    {
        // TODO: valós adatok lekérése
        _modelsData = new ObservableCollection<OllamaModel>();

        var ollamaModels = _modelsData as OllamaModel[] ?? _modelsData.ToArray();
        if (ollamaModels.Length == 0)
        {
            IsModelInfoBlockVisible = false;
            return;
        }

        Models = new ObservableCollection<OllamaModel>(ollamaModels);
        HasModelsToDisplay = Models.Count != 0;

        IsModelInfoBlockVisible = true;

        var downloadedModelsCount = ollamaModels.Count(m => m.DownloadStatus == ModelDownloadStatus.Downloaded);

        HasModelsToDisplay = true;
        HasDownloadedModels = downloadedModelsCount > 0;

        if (!HasDownloadedModels) return;

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

    // A rendezés beállítása alapján rendezi a modelleket
    private void SortModels()
    {
        if (Models.Count == 0) return;

        IEnumerable<OllamaModel> sortedModels;
        
        switch (SelectedSortingOption)
        {
            // ha nincs letöltött státuszban lévő model akkor visszaadja a simát
            // ha pedig van akkor külön veszi a letöltött modelleket a Models-ből majd összevonja egy olyan Models-el (amiből ki vannak véve a Downloaded elemek)
            // így előre kerülnek a letöltött státuszban lévők
            case SortingOption.Downloaded:
                sortedModels = Models.Any(m => m.DownloadStatus == ModelDownloadStatus.Downloaded)
                    ? Models.Where(m => m.DownloadStatus == ModelDownloadStatus.Downloaded)
                        .Concat(Models.Where(m => m.DownloadStatus != ModelDownloadStatus.Downloaded))
                    : Models;
                break;
            case SortingOption.ParametersAscending:
                sortedModels = Models.OrderBy(m => m.Parameters);
                break;
            case SortingOption.ParametersDescending:
                sortedModels = Models.OrderByDescending(m => m.Parameters);
                break;
            case SortingOption.SizeAscending:
                sortedModels = Models.OrderBy(m => m.Size);
                break;
            case SortingOption.SizeDescending:
                sortedModels = Models.OrderByDescending(m => m.Size);
                break;
            default:
                sortedModels = Models;
                break;
        }

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
    }

    // ez akkor hívódik meg ha a felhasználó valamelyik letöltéssel kapcsolatos interaktálható gombra kattint
    [RelayCommand]
    public async Task ModelAction(object parameter)
    {
        if (SelectedModel == null) return;
        if (parameter is ModelDownloadAction downloadAction)
        {
            switch (downloadAction)
            {
                case ModelDownloadAction.Start:
                    if (_downloadingModel != null)
                    {
                        _dialogService.ShowInfoDialog(LocalizationService.GetString("MULTIPLE_DOWNLOADS_WARNING"));
                        return;
                    }

                    SelectedModel.DownloadStatus = ModelDownloadStatus.Downloading;
                    await DownloadSelectedModelAsync();
                    break;
                case ModelDownloadAction.Pause:
                    SelectedModel.DownloadStatus = ModelDownloadStatus.Paused;
                    await _downloadCancellationTokenSource.CancelAsync();
                    break;
                case ModelDownloadAction.Resume:
                    if (_downloadingModel != null && _downloadingModel != SelectedModel)
                    {
                        _dialogService.ShowInfoDialog(LocalizationService.GetString("MULTIPLE_DOWNLOADS_WARNING"));
                        return;
                    }

                    if (SelectedModel.DownloadStatus == ModelDownloadStatus.Paused)
                    {
                        SelectedModel.DownloadStatus = ModelDownloadStatus.Downloading;
                        await DownloadSelectedModelAsync();
                    }

                    break;
                case ModelDownloadAction.Cancel:
                    SelectedModel.DownloadStatus = ModelDownloadStatus.Ready;
                    _downloadingModel = null;
                    DownloadedBytes = 0;
                    await _downloadCancellationTokenSource.CancelAsync();
                    break;
                case ModelDownloadAction.Delete:
                    var dialogResult = await _dialogService.ShowConfirmationDialog(
                        title: LocalizationService.GetString("CONFIRM_DELETION_DIALOG_TITLE"),
                        description: string.Format(LocalizationService.GetString("CONFIRM_DELETION_DIALOG_DESC"),
                            SelectedModel.Name),
                        positiveButtonText: LocalizationService.GetString("DELETE"),
                        negativeButtonText: LocalizationService.GetString("CANCEL"),
                        highlight: ConfirmationType.Positive
                    );

                    if (dialogResult is ConfirmationResult { Confirmation: ConfirmationType.Positive })
                    {
                        SelectedModel.DownloadStatus = ModelDownloadStatus.Ready;
                        DownloadedBytes = 0;
                        SortModels();
                    }

                    break;
            }
        }
    }

    // TODO: valósra átírni
    private async Task DownloadSelectedModelAsync()
    {
        if (SelectedModel == null) return;

        var random = new Random();

        _downloadCancellationTokenSource = new CancellationTokenSource();

        try
        {
            await Task.Run(async () =>
            {
                _downloadingModel = SelectedModel;
                while (DownloadedBytes != _downloadingModel.Size)
                {
                    // ez ellenőrzi hogy meg lett-e hívva a cancel a letöltésre, és ha igen kivételt dob
                    _downloadCancellationTokenSource.Token.ThrowIfCancellationRequested();

                    await Task.Delay(random.Next(200, 350));
                    var randomDownloadedBytes = random.Next(131072000, 209715200); // 125 MB - 200 MB
                    DownloadedBytes += randomDownloadedBytes;
                    DownloadSpeed = Math.Round((double)(randomDownloadedBytes * 8) / 1_000_000, 2);
                    if (DownloadedBytes > _downloadingModel.Size)
                    {
                        DownloadedBytes = _downloadingModel.Size;
                    }
                }

                _downloadingModel.DownloadStatus = ModelDownloadStatus.Downloaded;
                _downloadingModel = null;
                DownloadedBytes = 0;
                SortModels();
            }, _downloadCancellationTokenSource.Token);
        }
        catch (OperationCanceledException)
        {
            // ez akkor fut le ha a felhasználó szünetelteti a letöltést vagy visszavonja azt
            // TODO: letöltés visszavonása/szüneteltetése api-n keresztül, a _downloadingModel.DownloadStatus alapján
        }
        finally
        {
            _downloadCancellationTokenSource.Dispose();
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
    public void ShowInfo()
    {
        // TODO: tájékoztató a modelmanager működéséről, modellek információiról stb.
        _dialogService.ShowInfoDialog("ModelManager info here");
    }
}