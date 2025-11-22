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
    [ObservableProperty] private OllamaModel? _selectedModel;
    [ObservableProperty] private bool _isModelInfoBlockVisible;
    [ObservableProperty] private bool _isPaginationButtonVisible;

    [ObservableProperty] private double _downloadSpeed; // currently downloading model speed in Mbps

    [ObservableProperty] private long _downloadedBytes;

    private SortingOption _selectedSortingOption;

    // ez tudja észlelni ha egy task cancellelve van, kell egy tokenSource és annak a tokenjét beállítani a taskra
    // aztán kivételt dobva fogja megszakítani a taskot
    private CancellationTokenSource _downloadCancellationTokenSource = new();

    // currently downloading model
    private OllamaModel? _downloadingModel;

    public const int PaginationLimit = 50;
    private int _paginationIndex;

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
            FilterModels();
            OnPropertyChanged();
        }
    }

    public ModelManagerViewModel(IDialogService dialogService, IOllamaService ollamaService,
        IModelCacheService modelCacheService)
    {
        Page = ApplicationPage.ModelManager;
        _dialogService = dialogService;
        _ollamaService = ollamaService;
        _modelCacheService = modelCacheService;

        SelectedSortingOption = SortingOption.Downloaded;

        if (HasModelsToDisplay && (string.IsNullOrEmpty(SelectedModelName) || SelectedModel == null))
        {
            SelectedModelName = Models[0].Name;
            SelectedModel = Models[0];
        }
    }

    public async Task InitializeAsync()
    {
        await LoadModelsData();
    }

    private async Task LoadModelsData()
    {
        // Get models data from cache excluding cloud models
        // TODO: extract cloud models that has no "cloud" in their names but "cloud" among their labels
        _modelsData = (await _modelCacheService.GetCachedModelsAsync())
            .Where(m => !m.Name.EndsWith("-cloud", StringComparison.OrdinalIgnoreCase));

        // _modelsData = GenerateTestModels();

        var modelsData = _modelsData.ToList();
        IsPaginationButtonVisible = modelsData.Count > PaginationLimit;

        _paginationIndex = Math.Min(modelsData.Count, PaginationLimit);

        if (modelsData.Count == 0) return;

        Models = new ObservableCollection<OllamaModel>(modelsData.Take(Math.Min(modelsData.Count, PaginationLimit)));
        HasModelsToDisplay = Models.Count > 0;

        var downloadedModelsCount = modelsData.Count(m => m.DownloadStatus == ModelDownloadStatus.Downloaded);

        HasDownloadedModels = downloadedModelsCount > 0;

        if (!HasDownloadedModels) return;

        var downloadedSizeSum = modelsData
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
        if (!_modelsData.Any()) return;

        switch (SelectedSortingOption)
        {
            // ha nincs letöltött státuszban lévő model akkor visszaadja a simát
            // ha pedig van akkor külön veszi a letöltött modelleket a Models-ből majd összevonja egy olyan Models-el (amiből ki vannak véve a Downloaded elemek)
            // így előre kerülnek a letöltött státuszban lévők
            case SortingOption.Downloaded:
                _modelsData = _modelsData.Any(m => m.DownloadStatus == ModelDownloadStatus.Downloaded)
                    ? _modelsData.Where(m => m.DownloadStatus == ModelDownloadStatus.Downloaded)
                        .Concat(_modelsData.Where(m => m.DownloadStatus != ModelDownloadStatus.Downloaded))
                    : _modelsData;
                break;
            case SortingOption.PullCountAscending:
                _modelsData = _modelsData.OrderBy(m => m.Family?.PullCount);
                break;
            case SortingOption.PullCountDescending:
                _modelsData = _modelsData.OrderByDescending(m => m.Family?.PullCount);
                break;
            case SortingOption.SizeAscending:
                _modelsData = _modelsData.OrderBy(m => m.Size);
                break;
            case SortingOption.SizeDescending:
                _modelsData = _modelsData.OrderByDescending(m => m.Size);
                break;
        }

        ResetPagination(_modelsData);
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
            ;
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
                        if (!await _ollamaService.DeleteModel(SelectedModel.Name))
                        {
                            _dialogService.ShowErrorDialog(LocalizationService.GetString("ERROR_DELETING_MODEL"),
                                false);
                        }

                        DownloadedBytes = 0;
                        SortModels();
                    }

                    break;
            }
        }
    }

    // TODO: Fix UI inconsistencies with download
    private async Task DownloadSelectedModelAsync()
    {
        if (SelectedModel == null) return;

        _downloadCancellationTokenSource = new CancellationTokenSource();

        try
        {
            await Task.Run(async () =>
            {
                _downloadingModel = SelectedModel;
                while (_downloadingModel != null && DownloadedBytes != _downloadingModel.Size)
                {
                    // ez ellenőrzi hogy meg lett-e hívva a cancel a letöltésre, és ha igen kivételt dob
                    _downloadCancellationTokenSource.Token.ThrowIfCancellationRequested();

                    await foreach (var chunk in _ollamaService.PullModel(_downloadingModel.Name))
                    {
                        if (chunk.Total.HasValue && chunk.Completed.HasValue)
                        {
                            DownloadedBytes = chunk.Completed.Value;
                        }

                        DownloadSpeed = Math.Round((double)(DownloadedBytes * 8) / 1_000_000, 2);
                        if (_downloadingModel != null && DownloadedBytes > _downloadingModel.Size)
                        {
                            DownloadedBytes = _downloadingModel.Size;
                        }

                        if (chunk.Status == "success")
                        {
                            if (_downloadingModel != null)
                                _downloadingModel.DownloadStatus = ModelDownloadStatus.Downloaded;
                            _downloadingModel = null;
                            DownloadedBytes = 0;
                            SortModels();
                        }
                    }
                }
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

            IsModelInfoBlockVisible = true;

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

    public static int LevenshteinDistance(string s, string t)
    {
        if (string.IsNullOrEmpty(s))
            return t?.Length ?? 0;
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


    private int GetSearchMatchScore(string name, string search)
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

    public static List<OllamaModel> GenerateTestModels(int count = 50)
    {
        var rnd = new Random();

        var familyNames = new[] { "test-model", "alpha", "beta-llm", "neuro", "research", "proto" };
        var labelPool = new[] { "professional", "thinking", "lightweight", "experimental", "fast", "accurate" };
        var quantizations = new[] { "Q4_K_M", "Q5_K_S", "Q6_K_L", "FP8", "Q3_K_M" };

        var list = new List<OllamaModel>();

        for (int i = 0; i < count; i++)
        {
            // Random family
            var familyName = familyNames[rnd.Next(familyNames.Length)];
            var family = new OllamaModelFamily
            {
                Name = familyName,
                Description = "Lorem ipsum dolor sit amet consectetur, etc.",
                PullCount = rnd.Next(100_000, 5_000_000),
                Labels = labelPool.OrderBy(x => rnd.Next()).Take(rnd.Next(1, 4)).ToList(),
                LastUpdated = DateTime.Now.AddDays(-rnd.Next(0, 400)),
                TagCount = rnd.Next(1, 10)
            };

            // Random model name
            var parametersB = new[] { 3, 7, 8, 13, 20, 34 }[rnd.Next(6)];
            var quant = quantizations[rnd.Next(quantizations.Length)];
            var name = $"{familyName}:{parametersB}b-{quant}";

            var model = new OllamaModel
            {
                Name = name,
                Family = family,
                Size = rnd.NextInt64(10_000_000, 40_000_000), // 10MB–40MB
                Parameters = parametersB * 1_000_000L, // pl. 8b → 8M
                RunsSlow = rnd.Next(0, 2) == 0
            };

            list.Add(model);
        }

        return list;
    }
}
