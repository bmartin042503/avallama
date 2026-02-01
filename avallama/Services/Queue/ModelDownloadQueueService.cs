// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Threading;
using System.Threading.Tasks;
using avallama.Constants;
using avallama.Exceptions;
using avallama.Models.Download;
using avallama.Services.Ollama;
using avallama.Utilities;
using avallama.Utilities.Network;

namespace avallama.Services.Queue;

public interface IModelDownloadQueueService : IQueueService<ModelDownloadRequest>;

public class ModelModelDownloadQueueService : QueueService<ModelDownloadRequest>, IModelDownloadQueueService
{
    private readonly IOllamaService _ollamaService;
    private readonly INetworkManager _networkManager;

    public ModelModelDownloadQueueService(
        IOllamaService ollamaService,
        INetworkManager networkManager)
    {
        _ollamaService = ollamaService;
        _networkManager = networkManager;
    }

    protected override async Task ProcessItemAsync(ModelDownloadRequest request, CancellationToken ct)
    {
        if (!await _networkManager.IsInternetAvailableAsync())
        {
            throw new NoInternetConnectionException();
        }

        // TODO: add storage space check (this will be caught by QueueService.TryProcessItemAsync) and OnItemFailed will be called here

        request.DownloadPartCount = 1;
        await foreach (var chunk in _ollamaService.PullModelAsync(request.ModelName, ct))
        {
            if (chunk is { Total: not null, Completed: not null })
            {
                if (request.TotalBytes != 0 && chunk.Total.Value != request.TotalBytes)
                {
                    request.DownloadPartCount++;
                }

                var speed = request.SpeedCalculator.CalculateSpeed(chunk.Completed.Value);
                if (request.Status == null || request.Status.DownloadState == DownloadState.Queued)
                {
                    request.Status = new ModelDownloadStatus(DownloadState.Downloading);
                }

                request.DownloadedBytes = chunk.Completed.Value;
                request.TotalBytes = chunk.Total.Value;
                request.DownloadSpeed = speed;
            }

            if (chunk.Status == "success")
            {
                request.Status = new ModelDownloadStatus(DownloadState.Downloaded);
            }
        }
    }

    protected override void OnItemFailed(ModelDownloadRequest request, Exception ex)
    {
        string errorKey;
        string? arg = null;

        switch (ex)
        {
            case NoInternetConnectionException:
                errorKey = "NO_INTERNET_CONNECTION";
                break;

            // TODO: set model status to 'Paused' so the user can resume to the download when they have internet connection again
            case LostInternetConnectionException:
                errorKey = "LOST_INTERNET_CONNECTION";
                break;

            case OllamaLocalServerUnreachableException:
                errorKey = "OLLAMA_LOCAL_UNREACHABLE"; // TODO: add missing localization key
                break;

            case OllamaRemoteServerUnreachableException:
                errorKey = "OLLAMA_REMOTE_UNREACHABLE"; // TODO: add missing localization key
                break;

            case OllamaApiException apiEx:
                errorKey = "DOWNLOAD_FAILED";
                arg = apiEx.StatusCode.ToString();
                break;

            case InsufficientDiskSpaceException spaceEx:
                errorKey = "DISK_SPACE_INSUFFICIENT"; // TODO: add missing localization key
                arg = ConversionHelper.BytesToReadableSize(spaceEx.RequiredBytes);
                break;

            case DiskFullException:
                errorKey = "DISK_FULL_DURING_DOWNLOAD"; // TODO: add missing localization key
                break;

            default:
                errorKey = "DOWNLOAD_FAILED";
                arg = ex.Message;
                break;
        }

        var localizedMessage = LocalizationService.GetString(errorKey);

        if (!string.IsNullOrEmpty(arg))
        {
            try
            {
                localizedMessage = string.Format(localizedMessage, arg);
            }
            catch (FormatException)
            {
                localizedMessage += $" ({arg})";
            }
        }

        request.Status = new ModelDownloadStatus(DownloadState.Failed, localizedMessage);
    }

    protected override void OnItemCancelled(ModelDownloadRequest request)
    {
        request.DownloadSpeed = 0.0;
        request.SpeedCalculator.Reset();
    }
}
