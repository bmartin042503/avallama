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
using avallama.Utilities.Network;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace avallama.ViewModels;

public partial class ModelManagerViewModel : PageViewModel
{
    private readonly IDialogService _dialogService;
    private readonly IOllamaService _ollamaService;
    private readonly IModelCacheService _modelCacheService;
    private readonly INetworkManager _networkManager;

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

    // this can notice if a task is canceled, a tokenSource is needed and its token should be set to the task
    // then it will cancel the task by throwing an exception
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
        IModelCacheService modelCacheService, INetworkManager networkManager)
    {
        Page = ApplicationPage.ModelManager;
        _dialogService = dialogService;
        _ollamaService = ollamaService;
        _modelCacheService = modelCacheService;
        _networkManager = networkManager;

        _ollamaService.ServiceStateChanged += OllamaServiceStateChanged;
    }

    [RelayCommand]
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
            // if there is no model in downloaded status then it returns the normal one
            // if there is then it takes the downloaded ones separately from the rest and then concatenates them with a Models that has the Downloaded ones removed
            // thus the downloaded ones will be in the front
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
                Score = SearchUtilities.CalculateMatchScore(m.Name, search)
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
    public async Task ForwardToOllama(object parameter)
    {
        if (!await _networkManager.IsInternetAvailableAsync())
        {
            _dialogService.ShowErrorDialog(LocalizationService.GetString("NO_INTERNET_WARNING"), false);
            return;
        }

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

    // executes when the user interacts with a ModelItem (download, delete, pause, ...)
    [RelayCommand]
    public async Task ModelAction(object parameter)
    {
        if (SelectedModel == null) return;
        if (parameter is ModelDownloadAction downloadAction)
        {
            switch (downloadAction)
            {
                case ModelDownloadAction.Start:
                    if (!await _networkManager.IsInternetAvailableAsync())
                    {
                        _dialogService.ShowErrorDialog(LocalizationService.GetString("NO_INTERNET_WARNING"), false);
                        return;
                    }
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
                    if (!await _networkManager.IsInternetAvailableAsync())
                    {
                        _dialogService.ShowErrorDialog(LocalizationService.GetString("NO_INTERNET_WARNING"), false);
                        return;
                    }
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

                        await _ollamaService.UpdateDownloadedModels();

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

        var monitorTask = MonitorInternetAsync(_downloadCancellationTokenSource.Token);

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

                    // download is finished
                    if (chunk.Status == "success")
                    {
                        if (_downloadingModel != null)
                        {
                            _downloadingModel.DownloadStatus = ModelDownloadStatus.Downloaded;
                            await _ollamaService.UpdateDownloadedModels();
                            var downloadedModels = await _ollamaService.GetDownloadedModels();
                            var downloadedModel = downloadedModels.FirstOrDefault(m => m.Name == _downloadingModel.Name);
                            if (downloadedModel != null)
                            {
                                _downloadingModel.Info = downloadedModel.Info;
                                _downloadingModel.Size = downloadedModel.Size;
                                _downloadingModel.Parameters = downloadedModel.Parameters;
                            }
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
            try { await monitorTask; } catch (OperationCanceledException) { }

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

    private async Task MonitorInternetAsync(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(3), token);

                if (!await _networkManager.IsInternetAvailableAsync())
                {
                    _dialogService.ShowErrorDialog(LocalizationService.GetString("LOST_INTERNET_WARNING"), false);

                    // Pause the download, since we lost internet, but it can be resumed later
                    if (SelectedModel != null)
                    {
                        SelectedModel.DownloadStatus = ModelDownloadStatus.Paused;
                        await _modelCacheService.UpdateModelAsync(SelectedModel);
                        await _downloadCancellationTokenSource.CancelAsync();
                    }

                    return;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // expected when download stops
        }
    }

    // TODO: proper logging and implementation of status changes
    private void OllamaServiceStateChanged(ServiceState? state)
    {
        if (state == null) return;
        switch (state.Status)
        {
            case ServiceStatus.Running:
                break;
            case ServiceStatus.Retrying:
                break;
            case ServiceStatus.Stopped or ServiceStatus.Failed:
                break;
        }
    }
}
