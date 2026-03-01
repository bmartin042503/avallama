// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System.Threading.Tasks;
using avallama.Constants;
using avallama.Constants.States;
using avallama.Models.Download;
using avallama.Models.Ollama;
using avallama.Tests.Fixtures;
using avallama.ViewModels;
using Moq;
using Xunit;

namespace avallama.Tests.ViewModels;

public class ModelItemViewModelTests(TestServicesFixture fixture) : IClassFixture<TestServicesFixture>
{
    [Fact]
    public void CurrentStatus_WhenNoRequestAndNotDownloaded_ReturnsDownloadable()
    {
        var model = new OllamaModel { Name = "llama2", IsDownloaded = false };
        var vm = CreateViewModel(model);

        var status = vm.CurrentStatus;

        Assert.Equal(DownloadState.Downloadable, status.DownloadState);
    }

    [Fact]
    public void PauseCommand_WhenDownloading_PausesRequestAndCancelsToken()
    {
        var model = new OllamaModel { Name = "llama2" };
        var vm = CreateViewModel(model);
        vm.DownloadCommand.Execute(null);

        vm.DownloadRequest!.Status = new ModelDownloadStatus(DownloadState.Downloading);

        vm.PauseCommand.Execute(null);

        Assert.Equal(DownloadState.Paused, vm.DownloadRequest.Status?.DownloadState);
        Assert.Equal(QueueItemCancellationReason.UserPauseRequest, vm.DownloadRequest.QueueItemCancellationReason);
        Assert.True(vm.DownloadRequest.Token.IsCancellationRequested);
    }

    [Fact]
    public void CancelCommand_WhenDownloading_CancelsRequestAndRemovesIt()
    {
        var model = new OllamaModel { Name = "llama2" };
        var vm = CreateViewModel(model);
        vm.DownloadCommand.Execute(null);
        var oldRequest = vm.DownloadRequest;

        vm.CancelCommand.Execute(null);

        Assert.Null(vm.DownloadRequest);
        Assert.True(oldRequest!.Token.IsCancellationRequested);
        Assert.Equal(QueueItemCancellationReason.UserCancelRequest, oldRequest.QueueItemCancellationReason);
    }

    [Fact]
    public async Task DownloadCompletedEvent_UpdatesDatabaseAndClearsRequest()
    {
        fixture.OllamaMock.Reset();
        fixture.ModelCacheMock.Reset();
        fixture.DialogMock.Reset();

        var model = new OllamaModel { Name = "llama2", IsDownloaded = false };
        var vm = CreateViewModel(model);

        var tcs = new TaskCompletionSource();

        fixture.ModelCacheMock
            .Setup(m => m.ContainsModelAsync(model))
            .ReturnsAsync(false);

        fixture.ModelCacheMock
            .Setup(m => m.InsertModelAsync(model))
            .Returns(Task.CompletedTask)
            .Callback(() => tcs.SetResult());

        vm.DownloadCommand.Execute(null);

        vm.DownloadRequest!.Status = new ModelDownloadStatus(DownloadState.Downloaded);

        await tcs.Task;

        Assert.True(vm.Model.IsDownloaded);
        Assert.Null(vm.DownloadRequest);

        fixture.OllamaMock.Verify(o => o.EnrichModelAsync(model), Times.Once);
        fixture.ModelCacheMock.Verify(m => m.InsertModelAsync(model), Times.Once);
        fixture.DialogMock.Verify(d => d.ShowErrorDialog(It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public void DownloadFailedEvent_ShowsErrorDialog()
    {
        fixture.DialogMock.Reset();
        var model = new OllamaModel { Name = "llama3:1b" };
        var vm = CreateViewModel(model);
        vm.DownloadCommand.Execute(null);

        vm.DownloadRequest!.Status = new ModelDownloadStatus(DownloadState.Failed, "No internet connection.");

        fixture.DialogMock.Verify(d => d.ShowErrorDialog("No internet connection.", false), Times.Once);
        Assert.NotNull(vm.DownloadRequest);
    }

    // TODO: implement missing test cases such (e.g. status text updates, model deletion etc.)

    private ModelItemViewModel CreateViewModel(OllamaModel model)
    {
        return new ModelItemViewModel(
            model,
            fixture.OllamaMock.Object,
            fixture.DownloadQueueMock.Object,
            fixture.DialogMock.Object,
            fixture.ModelCacheMock.Object,
            fixture.MessengerMock.Object
        );
    }
}
