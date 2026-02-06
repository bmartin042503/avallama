// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System.Threading.Tasks;
using avallama.Constants;
using avallama.Constants.Application;
using avallama.Constants.States;
using avallama.Models.Download;
using avallama.Models.Ollama;
using avallama.Services;
using avallama.Services.Ollama;
using avallama.Services.Queue;
using avallama.Utilities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

namespace avallama.ViewModels;

public partial class ModelItemViewModel : ViewModelBase
{
    private readonly IOllamaService _ollamaService;
    private readonly IModelDownloadQueueService _modelDownloadQueueService;
    private readonly IDialogService _dialogService;
    private readonly IMessenger _messenger;

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
        IDialogService dialogService,
        IMessenger messenger)
    {
        Model = model;
        _ollamaService = ollamaService;
        _modelDownloadQueueService = modelDownloadQueueService;
        _dialogService = dialogService;
        _messenger = messenger;
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
        _messenger.Send(new ApplicationMessage.ModelStatusChanged(Model.Name));
    }

    [RelayCommand]
    public void Pause()
    {
        if (DownloadRequest == null ||
            DownloadRequest.Status == null ||
            DownloadRequest.Status.DownloadState == DownloadState.Paused) return;

        DownloadRequest?.Cancel();
        DownloadRequest?.QueueItemCancellationReason = QueueItemCancellationReason.UserPauseRequest;
        DownloadRequest?.Status = new ModelDownloadStatus(DownloadState.Paused);
        _messenger.Send(new ApplicationMessage.ModelStatusChanged(Model.Name));
    }

    [RelayCommand]
    public void Resume()
    {
        if (DownloadRequest == null ||
            DownloadRequest.Status == null ||
            DownloadRequest.Status.DownloadState != DownloadState.Paused) return;

        DownloadRequest.QueueItemCancellationReason = QueueItemCancellationReason.Unknown;
        DownloadRequest.ResetToken();
        _modelDownloadQueueService.Enqueue(DownloadRequest);
        DownloadRequest.Status = new ModelDownloadStatus(DownloadState.Queued);
        _messenger.Send(new ApplicationMessage.ModelStatusChanged(Model.Name));
    }

    [RelayCommand]
    public void Cancel()
    {
        if (DownloadRequest == null) return;
        DownloadRequest.Cancel();
        DownloadRequest.QueueItemCancellationReason = QueueItemCancellationReason.UserCancelRequest;
        DownloadRequest = null;
        OnPropertyChanged(nameof(CurrentStatus));
        _messenger.Send(new ApplicationMessage.ModelStatusChanged(Model.Name));
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
            if (await _ollamaService.DeleteModelAsync(Model.Name))
            {
                Model.IsDownloaded = false;
                DownloadRequest = null;
                await _ollamaService.UpdateDownloadedModelsAsync();
                OnPropertyChanged(nameof(CurrentStatus));
                _messenger.Send(new ApplicationMessage.ModelStatusChanged(Model.Name));
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
                if (DownloadRequest?.Status == null) return;
                OnPropertyChanged(nameof(CurrentStatus));

                switch (DownloadRequest.Status.DownloadState)
                {
                    case DownloadState.Downloaded:
                        Model.IsDownloaded = true;
                        DownloadRequest = null;
                        _messenger.Send(new ApplicationMessage.ModelStatusChanged(Model.Name));
                        break;
                    case DownloadState.Failed:
                        _dialogService.ShowErrorDialog(
                            DownloadRequest.Status.Message ?? LocalizationService.GetString("UNKNOWN_ERROR"),
                            false);
                        break;
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
