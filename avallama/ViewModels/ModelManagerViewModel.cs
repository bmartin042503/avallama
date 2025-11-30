// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
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

public partial class ModelManagerViewModel : PageViewModel
{
    private readonly IDialogService _dialogService;
    private readonly IOllamaService _ollamaService;
    private readonly IModelCacheService _modelCacheService;

    // all loaded models (stays in the memory)
    private IEnumerable<OllamaModel> _modelsData = [];

    // filtered models
    private IEnumerable<OllamaModel> _filteredModelsData = [];

    // model sorting options
    public IEnumerable<SortingOption> SortingOptions { get; } = Enum.GetValues<SortingOption>();

    // rendered models on the UI
    [ObservableProperty] private ObservableCollection<OllamaModel> _models = [];

    [ObservableProperty] private string _downloadedModelsInfo = string.Empty;
    [ObservableProperty] private bool _hasDownloadedModels;
    [ObservableProperty] private bool _hasModelsToDisplay;
    [ObservableProperty] private string _selectedModelName = string.Empty;
    [ObservableProperty] private OllamaModel? _selectedModel = new();
    [ObservableProperty] private bool _isModelInfoBlockVisible;
    [ObservableProperty] private bool _isPaginationButtonVisible;

    private Timer? _downloadSpeedUpdateTimer;
    private int _downloadingPartCount;
    private double _downloadSpeed;
    private long _downloadedBytes;
    private long _bytesToDownload;

    [ObservableProperty] private string _downloadStatusText = string.Empty;
    [ObservableProperty] private string _downloadSpeedText = "0 MB/s";

    // ez tudja észlelni ha egy task cancellelve van, kell egy tokenSource és annak a tokenjét beállítani a taskra
    // aztán kivételt dobva fogja megszakítani a taskot
    private CancellationTokenSource _downloadCancellationTokenSource = new();

    // currently downloading model
    private OllamaModel? _downloadingModel;

    public const int PaginationLimit = 50;
    private int _paginationIndex;

    public SortingOption SelectedSortingOption
    {
        get;
        set
        {
            field = value;
            SortModels();
            OnPropertyChanged();
        }
    }

    public string SearchBoxText
    {
        get;
        set
        {
            field = value;
            FilterModels();
            OnPropertyChanged();
        }
    } = string.Empty;

    public ModelManagerViewModel(IDialogService dialogService, IOllamaService ollamaService,
        IModelCacheService modelCacheService)
    {
        Page = ApplicationPage.ModelManager;
        _dialogService = dialogService;
        _ollamaService = ollamaService;
        _modelCacheService = modelCacheService;
    }

    public async Task InitializeAsync()
    {
        await LoadModelsData();
        SelectedSortingOption = SortingOption.Downloaded;
    }

    private async Task LoadModelsData()
    {
        // Get models data from cache excluding cloud models
        // TODO: extract cloud models that has "cloud" in their names or "cloud" among their labels
        _modelsData = (await _modelCacheService.GetCachedModelsAsync())
            .Where(m => !m.Name.EndsWith("-cloud", StringComparison.OrdinalIgnoreCase));

        var modelsData = _modelsData.ToList();
        IsPaginationButtonVisible = modelsData.Count > PaginationLimit;

        _paginationIndex = Math.Min(modelsData.Count, PaginationLimit);

        if (modelsData.Count == 0) return;

        SortModels();

        Models = new ObservableCollection<OllamaModel>(modelsData.Take(Math.Min(modelsData.Count, PaginationLimit)));
        HasModelsToDisplay = Models.Count > 0;

        var downloadedModelsCount = modelsData.Count(m => m.DownloadStatus == ModelDownloadStatus.Downloaded);

        HasDownloadedModels = downloadedModelsCount > 0;

        if (!HasDownloadedModels) return;

        UpdateDownloadedModelsInfo();
    }

    // A rendezés beállítása alapján rendezi a modelleket
    private void SortModels()
    {
        var search = SearchBoxText.Trim();
        var hasSearch = !string.IsNullOrEmpty(search);
        var dataToSort = hasSearch ? _filteredModelsData : _modelsData;
        var models = dataToSort.ToList();

        if (models.Count == 0) return;

        IEnumerable<OllamaModel> sortResult = models;

        switch (SelectedSortingOption)
        {
            // ha nincs letöltött státuszban lévő model akkor visszaadja a simát
            // ha pedig van akkor külön veszi a letöltött modelleket a Models-ből majd összevonja egy olyan Models-el (amiből ki vannak véve a Downloaded elemek)
            // így előre kerülnek a letöltött státuszban lévők
            case SortingOption.Downloaded:
                sortResult = models.Any(m => m.DownloadStatus == ModelDownloadStatus.Downloaded)
                    ? models.Where(m => m.DownloadStatus == ModelDownloadStatus.Downloaded)
                        .Concat(models.Where(m => m.DownloadStatus != ModelDownloadStatus.Downloaded))
                    : models;
                break;
            case SortingOption.PullCountAscending:
                sortResult = models.OrderBy(m => m.Family?.PullCount);
                break;
            case SortingOption.PullCountDescending:
                sortResult = models.OrderByDescending(m => m.Family?.PullCount);
                break;
            case SortingOption.SizeAscending:
                sortResult = models.OrderBy(m => m.Size);
                break;
            case SortingOption.SizeDescending:
                sortResult = models.OrderByDescending(m => m.Size);
                break;
        }

        if (hasSearch)
        {
            _filteredModelsData = sortResult;
        }
        else
        {
            _modelsData = sortResult;
        }

        ResetPagination(hasSearch ? _filteredModelsData : _modelsData);
    }

    // Filters models by the search text and sets the _filteredModelsData
    private void FilterModels()
    {
        if (!_modelsData.Any())
        {
            HasModelsToDisplay = false;
            return;
        }

        var search = SearchBoxText.Trim();
        if (string.IsNullOrEmpty(search))
        {
            ResetPagination(_modelsData);
            return;
        }

        _filteredModelsData = _modelsData
            .Select(m => new
            {
                Model = m,
                Score = GetSearchMatchScore(m.Name, search)
            })
            .Where(x => x.Score >= 25)
            .OrderByDescending(x => x.Score)
            .Select(x => x.Model);

        var filteredModelsData = _filteredModelsData.ToList();

        ResetPagination(filteredModelsData);

        HasModelsToDisplay = filteredModelsData.Count > 0;
    }

    private void ResetPagination(IEnumerable<OllamaModel> models)
    {
        var modelsList = models.ToList();
        Models = new ObservableCollection<OllamaModel>(modelsList.Take(Math.Min(PaginationLimit, modelsList.Count)));
        _paginationIndex = Math.Min(PaginationLimit, modelsList.Count);
        IsPaginationButtonVisible = modelsList.Count > PaginationLimit;
    }

    [RelayCommand]
    public void ForwardToOllama(object parameter)
    {
        if (parameter is string modelName)
        {
            const string libraryUrl = @"https://ollama.com/library/";

            Process.Start(new ProcessStartInfo
            {
                FileName = libraryUrl + modelName,
                UseShellExecute = true
            });
        }
    }

    [RelayCommand]
    public void Paginate()
    {
        var search = SearchBoxText.Trim();
        var hasSearch = !string.IsNullOrEmpty(search);
        var dataToPaginate = hasSearch ? _filteredModelsData.ToList() : _modelsData.ToList();

        if (dataToPaginate.Count == 0) return;

        if (_paginationIndex < dataToPaginate.Count)
        {
            var modelsToAdd = Math.Min(PaginationLimit, dataToPaginate.Count - _paginationIndex);
            foreach (var model in dataToPaginate.Skip(_paginationIndex).Take(modelsToAdd))
            {
                Models.Add(model);
                _paginationIndex++;
            }
        }

        IsPaginationButtonVisible = _paginationIndex < dataToPaginate.Count;
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
                    await _modelCacheService.UpdateModelAsync(SelectedModel);
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
                    _downloadedBytes = 0;
                    _bytesToDownload = 0;
                    _downloadSpeed = 0.0;
                    await _downloadCancellationTokenSource.CancelAsync();
                    break;
                case ModelDownloadAction.Delete:
                    var dialogResult = await _dialogService.ShowConfirmationDialog(
                        LocalizationService.GetString("CONFIRM_DELETION_DIALOG_TITLE"),
                        LocalizationService.GetString("DELETE"),
                        LocalizationService.GetString("CANCEL"),
                        string.Format(LocalizationService.GetString("CONFIRM_DELETION_DIALOG_DESC"),
                            SelectedModel.Name),
                        ConfirmationType.Positive
                    );

                    if (dialogResult is ConfirmationResult { Confirmation: ConfirmationType.Positive })
                    {
                        SelectedModel.DownloadStatus = ModelDownloadStatus.Ready;
                        if (!await _ollamaService.DeleteModel(SelectedModel.Name))
                        {
                            _dialogService.ShowErrorDialog(LocalizationService.GetString("ERROR_DELETING_MODEL"),
                                false);
                        }

                        await _modelCacheService.UpdateModelAsync(SelectedModel);

                        _downloadedBytes = 0;
                        SortModels();
                        UpdateDownloadedModelsInfo();
                    }

                    break;
            }
        }
    }

    private async Task DownloadSelectedModelAsync()
    {
        if (SelectedModel == null) return;

        _downloadCancellationTokenSource = new CancellationTokenSource();

        try
        {
            await Task.Run(async () =>
            {
                var networkSpeedCalculator = new NetworkSpeedCalculator();
                _downloadingModel = SelectedModel;
                _downloadingPartCount = 1;

                _downloadSpeedUpdateTimer = new Timer(UpdateDownloadSpeedText, null, TimeSpan.FromMilliseconds(1000),
                    TimeSpan.FromMilliseconds(1000));

                await foreach (var chunk in _ollamaService.PullModel(_downloadingModel.Name))
                {
                    _downloadCancellationTokenSource.Token.ThrowIfCancellationRequested();

                    if (chunk is { Total: not null, Completed: not null })
                    {
                        if (_bytesToDownload != 0 && chunk.Total.Value != _bytesToDownload)
                        {
                            _downloadingPartCount++;
                        }

                        _bytesToDownload = chunk.Total.Value;
                        _downloadedBytes = chunk.Completed.Value;
                    }

                    _downloadSpeed = networkSpeedCalculator.CalculateSpeed(chunk.Completed ?? 0);

                    DownloadStatusText = string.Format(LocalizationService.GetString("DOWNLOADING_PART"),
                        _downloadingPartCount);
                    DownloadStatusText +=
                        $" - {ConversionHelper.BytesToReadableSize(_downloadedBytes)}/{ConversionHelper.BytesToReadableSize(_bytesToDownload)}";

                    if (chunk.Status == "success")
                    {
                        if (_downloadingModel != null)
                        {
                            _downloadingModel.DownloadStatus = ModelDownloadStatus.Downloaded;
                            await _modelCacheService.UpdateModelAsync(_downloadingModel);
                        }

                        await _downloadSpeedUpdateTimer.DisposeAsync();
                        _downloadingModel = null;
                        _downloadedBytes = 0;
                        _bytesToDownload = 0;
                        _downloadSpeed = 0.0;
                        SortModels();

                        UpdateDownloadedModelsInfo();
                    }
                }
            }, _downloadCancellationTokenSource.Token);
        }
        catch (OperationCanceledException)
        {
            _downloadSpeedUpdateTimer?.Dispose();
        }
        finally
        {
            _downloadCancellationTokenSource.Dispose();
        }
    }

    private void UpdateDownloadedModelsInfo()
    {
        var downloadedModelsCount = _modelsData.Count(m => m.DownloadStatus == ModelDownloadStatus.Downloaded);
        var downloadedSizeSum = _modelsData
            .Where(m => m.DownloadStatus == ModelDownloadStatus.Downloaded)
            .Sum(m => m.Size);

        DownloadedModelsInfo =
            string.Format(
                LocalizationService.GetString("DOWNLOADED_MODELS"),
                downloadedModelsCount
            );

        DownloadedModelsInfo += $" ({ConversionHelper.BytesToReadableSize(downloadedSizeSum)})";
    }

    [RelayCommand]
    public void SelectModel(object parameter)
    {
        if (parameter is string modelName)
        {
            if (string.IsNullOrEmpty(modelName)) return;
            SelectedModelName = modelName;

            IsModelInfoBlockVisible = true;

            var modelFromName = Models.FirstOrDefault(m => m.Name == SelectedModelName);
            if (modelFromName != null) SelectedModel = modelFromName;
        }
    }

    [RelayCommand]
    public void ShowInfo()
    {
        _dialogService.ShowInfoDialog(LocalizationService.GetString("MODEL_MANAGER_GUIDE"));
    }

    private void UpdateDownloadSpeedText(object? state)
    {
        DownloadSpeedText = $"{_downloadSpeed:0.##} MB/s";
    }

    private static int LevenshteinDistance(string s, string t)
    {
        if (string.IsNullOrEmpty(s))
            return t.Length;
        if (string.IsNullOrEmpty(t))
            return s.Length;

        var d = new int[s.Length + 1, t.Length + 1];

        for (var i = 0; i <= s.Length; i++)
            d[i, 0] = i;

        for (var j = 0; j <= t.Length; j++)
            d[0, j] = j;

        for (var i = 1; i <= s.Length; i++)
        {
            for (var j = 1; j <= t.Length; j++)
            {
                var cost = s[i - 1] == t[j - 1] ? 0 : 1;

                d[i, j] = Math.Min(
                    Math.Min(
                        d[i - 1, j] + 1,
                        d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost
                );
            }
        }

        return d[s.Length, t.Length];
    }


    private static int GetSearchMatchScore(string name, string search)
    {
        if (string.IsNullOrWhiteSpace(search))
            return 0;

        name = name.ToLowerInvariant();
        search = search.ToLowerInvariant();

        var score = 0;

        if (name.StartsWith(search))
            score += 100;

        if (name.Contains(search))
            score += 40;

        // fuzzy, looks for typos
        int lev = LevenshteinDistance(name, search);

        // the least the distance the better it is (max 30 score)
        var fuzzyScore = Math.Max(0, 30 - lev);
        score += fuzzyScore;

        return score;
    }
}
