// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System.Threading.Tasks;
using avallama.Constants;
using avallama.Models.Download;
using avallama.Models.Ollama;
using avallama.Services;
using avallama.Services.Ollama;
using avallama.Services.Queue;
using avallama.Utilities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace avallama.ViewModels;

public partial class ModelItemViewModel : ViewModelBase
{
    private readonly IOllamaService _ollamaService;
    private readonly IModelDownloadQueueService _modelDownloadQueueService;
    private readonly IDialogService _dialogService;

    [ObservableProperty] private OllamaModel _model;

    // a download request that can be used in the queue
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(CurrentStatus))]
    private ModelDownloadRequest? _downloadRequest;

    // short download status text when the viewmodel is used inside a ModelListItem
    [ObservableProperty] private string _shortDownloadStatusText = string.Empty;

    // longer download status text when the viewmodel is used inside a ModelItem
    [ObservableProperty] private string _longDownloadStatusText = string.Empty;

    [ObservableProperty] private string _downloadSpeedText = string.Empty;

    public ModelItemViewModel(
        OllamaModel model,
        IOllamaService ollamaService,
        IModelDownloadQueueService modelDownloadQueueService,
        IDialogService dialogService)
    {
        Model = model;
        _ollamaService = ollamaService;
        _modelDownloadQueueService = modelDownloadQueueService;
        _dialogService = dialogService;
    }

    // computed property which provides the status of the model itself
    // it uses the downloadrequest's current status or the model's status if there is no active request
    public ModelDownloadStatus CurrentStatus
    {
        get
        {
            ModelDownloadStatus returnedStatus;

            if (DownloadRequest is { Status: not null })
            {
                returnedStatus = DownloadRequest.Status;
            }
            else
            {
                returnedStatus = Model.IsDownloaded
                    ? new ModelDownloadStatus(DownloadState.Downloaded)
                    : new ModelDownloadStatus(); // Downloadable
            }

            return returnedStatus;
        }
    }

    [RelayCommand]
    public void Download()
    {
        if (Model.IsDownloaded) return;
        DownloadRequest = new ModelDownloadRequest
        {
            ModelName = Model.Name,
            Status = new ModelDownloadStatus(DownloadState.Queued),
        };
        _modelDownloadQueueService.Enqueue(DownloadRequest);
    }

    [RelayCommand]
    public void Pause()
    {
        if (DownloadRequest == null ||
            DownloadRequest.Status == null ||
            DownloadRequest.Status.DownloadState == DownloadState.Paused) return;

        DownloadRequest?.Cancel();
        DownloadRequest?.Status = new ModelDownloadStatus(DownloadState.Paused);
    }

    [RelayCommand]
    public void Resume()
    {
        if (DownloadRequest == null ||
            DownloadRequest.Status == null ||
            DownloadRequest.Status.DownloadState != DownloadState.Paused) return;

        _modelDownloadQueueService.Enqueue(DownloadRequest);
        DownloadRequest.Status = new ModelDownloadStatus(DownloadState.Queued);
    }

    [RelayCommand]
    public void Cancel()
    {
        if (DownloadRequest == null) return;
        DownloadRequest.Cancel();
        DownloadRequest = null;
        OnPropertyChanged(nameof(CurrentStatus));
    }

    [RelayCommand]
    public async Task Delete()
    {
        if (!Model.IsDownloaded) return;
        var dialogResult = await _dialogService.ShowConfirmationDialog(
            LocalizationService.GetString("CONFIRM_DELETION_DIALOG_TITLE"),
            LocalizationService.GetString("DELETE"),
            LocalizationService.GetString("CANCEL"),
            string.Format(LocalizationService.GetString("CONFIRM_DELETION_DIALOG_DESC"),
                Model.Name),
            ConfirmationType.Positive
        );

        if (dialogResult is ConfirmationResult { Confirmation: ConfirmationType.Positive })
        {
            if (await _ollamaService.DeleteModel(Model.Name))
            {
                Model.IsDownloaded = false;
                DownloadRequest = null;
                await _ollamaService.UpdateDownloadedModels();

                // TODO: notify ModelManagerViewModel to resort the models list and update downloaded models info
            }
            else
            {
                _dialogService.ShowErrorDialog(LocalizationService.GetString("ERROR_DELETING_MODEL"),
                    false);
            }
        }
    }

    // this is called inside the active download request's setter (which is generated automatically)
    partial void OnDownloadRequestChanged(ModelDownloadRequest? oldValue, ModelDownloadRequest? newValue)
    {
        if (oldValue != null)
        {
            oldValue.PropertyChanged -= OnDownloadRequestPropertyChanged;
        }

        if (newValue != null)
        {
            newValue.PropertyChanged += OnDownloadRequestPropertyChanged;

            UpdateStatusTexts();
        }
        else
        {
            ShortDownloadStatusText = string.Empty;
            LongDownloadStatusText = string.Empty;
        }
    }

    private void OnDownloadRequestPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(DownloadRequest.Status):
            {
                OnPropertyChanged(nameof(CurrentStatus));

                if (DownloadRequest?.Status?.DownloadState == DownloadState.Downloaded)
                {
                    Model.IsDownloaded = true;
                    DownloadRequest = null;
                }

                break;
            }
            case nameof(DownloadRequest.DownloadedBytes) or nameof(DownloadRequest.DownloadSpeed):
                UpdateStatusTexts();
                break;
        }
    }

    private void UpdateStatusTexts()
    {
        if (DownloadRequest?.Status == null) return;

        switch (DownloadRequest.DownloadPartCount)
        {
            case > 1:
                ShortDownloadStatusText = LocalizationService.GetString("FINALIZING");
                LongDownloadStatusText = LocalizationService.GetString("FINALIZING_DOWNLOAD");
                break;
            case 1:
            {
                var downloadedBytes = DownloadRequest.DownloadedBytes;
                var totalBytes = DownloadRequest.TotalBytes;

                if (totalBytes > 0)
                {
                    var percentage = (double)downloadedBytes / totalBytes * 100;
                    ShortDownloadStatusText = $"{percentage:F2}%";
                }
                else
                {
                    ShortDownloadStatusText = "0%";
                }
                LongDownloadStatusText = LocalizationService.GetString("DOWNLOADING");
                LongDownloadStatusText +=
                    $" - {ConversionHelper.BytesToReadableSize(downloadedBytes)}/{ConversionHelper.BytesToReadableSize(totalBytes)}";

                DownloadSpeedText = $"{DownloadRequest.DownloadSpeed:0.##} MB/s";
                break;
            }
            default:
                ShortDownloadStatusText = string.Empty;
                LongDownloadStatusText = string.Empty;
                break;
        }
    }
}
