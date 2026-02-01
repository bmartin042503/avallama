// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using avallama.Constants;
using avallama.Models.Download;
using avallama.Models.Ollama;
using avallama.Tests.Fixtures;
using avallama.ViewModels;
using Moq;
using Xunit;

namespace avallama.Tests.ViewModels;

public class ModelManagerViewModelTests(TestServicesFixture fixture) : IClassFixture<TestServicesFixture>
{
    private void SetupMocks()
    {
        fixture.ConfigMock.Reset();
        fixture.DownloadQueueMock.Reset();
        fixture.ModelCacheMock.Reset();

        fixture.ConfigMock
            .Setup(c => c.ReadSetting(ConfigurationKey.IsParallelDownloadEnabled))
            .Returns("False");

        fixture.DownloadQueueMock
            .Setup(q => q.SetParallelism(It.IsAny<int>()));
    }

    private ModelManagerViewModel CreateViewModel()
    {
        return new ModelManagerViewModel(
            fixture.DialogMock.Object,
            fixture.OllamaMock.Object,
            fixture.ModelCacheMock.Object,
            fixture.NetworkManagerMock.Object,
            fixture.ConfigMock.Object,
            fixture.DownloadQueueMock.Object
        );
    }

    private static List<OllamaModel> CreateModels(int count)
    {
        var models = new List<OllamaModel>();
        for (var i = 0; i < count; i++)
        {
            models.Add(
                new OllamaModel
                {
                    Name = "Name-" + i,
                    Size = i * 1000_000_000,
                    Family = new OllamaModelFamily
                    {
                        Name = "Family-" + i,
                        Description = "Desc-" + i,
                    }
                }
            );
        }

        return models;
    }

    [Fact]
    public async Task InitializesModelManager_NoModelSelected_ModelItemIsInvisible()
    {
        SetupMocks();

        var models = CreateModels(50);

        fixture.ModelCacheMock
            .Setup(o => o.GetCachedModelsAsync())
            .ReturnsAsync(models);

        var vm = CreateViewModel();

        await vm.InitializeAsync();

        Assert.Null(vm.SelectedModelItemViewModel);
        Assert.False(vm.IsModelInfoBlockVisible);
        Assert.Equal(models.Count, vm.ModelItemViewModels.Count);
        Assert.True(vm.HasModelsToDisplay);

        fixture.DownloadQueueMock.Verify(q => q.SetParallelism(It.IsAny<int>()), Times.Once);
    }


    [Fact]
    public async Task LoadModelsData_HasModelsInMemory_ExceedsPaginationLimit_ShouldExposeOnlyLimitedNumberOfModels()
    {
        SetupMocks();

        var models = CreateModels(75);

        fixture.ModelCacheMock
            .Setup(o => o.GetCachedModelsAsync())
            .ReturnsAsync(models);

        var vm = CreateViewModel();

        await vm.InitializeAsync();

        Assert.Equal(ModelManagerViewModel.PaginationLimit, vm.ModelItemViewModels.Count);
        Assert.True(vm.IsPaginationButtonVisible);
    }

    [Fact]
    public async Task LoadModelsData_HasModelsInMemory_BelowPaginationLimit_ShouldExposeAllModels()
    {
        SetupMocks();

        var models = CreateModels(30);

        fixture.ModelCacheMock
            .Setup(o => o.GetCachedModelsAsync())
            .ReturnsAsync(models);

        var vm = CreateViewModel();

        await vm.InitializeAsync();

        Assert.Equal(models.Count, vm.ModelItemViewModels.Count);
        Assert.False(vm.IsPaginationButtonVisible);
    }

    [Fact]
    public async Task LoadModelsData_Paginate_UntilPaginationLimitReached()
    {
        SetupMocks();

        var models = CreateModels(120);

        fixture.ModelCacheMock
            .Setup(o => o.GetCachedModelsAsync())
            .ReturnsAsync(models);

        var vm = CreateViewModel();

        await vm.InitializeAsync();

        Assert.Equal(ModelManagerViewModel.PaginationLimit, vm.ModelItemViewModels.Count);
        Assert.True(vm.IsPaginationButtonVisible);

        while (vm.IsPaginationButtonVisible)
        {
            vm.Paginate();
        }

        Assert.Equal(models.Count, vm.ModelItemViewModels.Count);
        Assert.False(vm.IsPaginationButtonVisible);
    }

    [Fact]
    public async Task Search_WithMatchingResults_ShouldFilterModels()
    {
        SetupMocks();

        var models = new List<OllamaModel>
        {
            new() { Name = "llama-3", Size = 1_000_000_000, IsDownloaded = false },
            new() { Name = "llama-3-instruct", Size = 2_000_000_000, IsDownloaded = false },
            new() { Name = "mistral", Size = 3_000_000_000, IsDownloaded = false },
            new() { Name = "gemma-21b", Size = 21_000_000_000, IsDownloaded = false },
            new() { Name = "deepseek-r1:4b", Size = 4_000_000_000, IsDownloaded = false },
            new() { Name = "qwen:6b", Size = 6_000_000_000, IsDownloaded = false }
        };

        fixture.ModelCacheMock
            .Setup(o => o.GetCachedModelsAsync())
            .ReturnsAsync(models);

        var vm = CreateViewModel();

        await vm.InitializeAsync();

        vm.SearchBoxText = "mistral";
        Assert.True(vm.HasModelsToDisplay);
        Assert.Single(vm.ModelItemViewModels);
        Assert.Equal("mistral", vm.ModelItemViewModels[0].Model.Name);

        vm.SearchBoxText = "llama";
        Assert.True(vm.HasModelsToDisplay);
        Assert.Equal(2, vm.ModelItemViewModels.Count);
        Assert.Equal("llama-3", vm.ModelItemViewModels[0].Model.Name);
        Assert.Equal("llama-3-instruct", vm.ModelItemViewModels[1].Model.Name);

        vm.SearchBoxText = "d3rpsfek";
        Assert.False(vm.HasModelsToDisplay);
        Assert.Empty(vm.ModelItemViewModels);

        vm.SearchBoxText = "ma";
        Assert.True(vm.HasModelsToDisplay);
        Assert.Equal(4, vm.ModelItemViewModels.Count);
        Assert.Equal("llama-3", vm.ModelItemViewModels[0].Model.Name);
        Assert.Equal("gemma-21b", vm.ModelItemViewModels[1].Model.Name);
        Assert.Equal("llama-3-instruct", vm.ModelItemViewModels[2].Model.Name);
        Assert.Equal("mistral", vm.ModelItemViewModels[3].Model.Name);
    }

    [Fact]
    public async Task Search_WithNoResults_ShouldExposeNoModels()
    {
        SetupMocks();

        var models = new List<OllamaModel>
        {
            new() { Name = "llama-3", Size = 1_000_000_000, IsDownloaded = false },
            new() { Name = "mistral", Size = 3_000_000_000, IsDownloaded = false }
        };

        fixture.ModelCacheMock
            .Setup(o => o.GetCachedModelsAsync())
            .ReturnsAsync(models);

        var vm = CreateViewModel();

        await vm.InitializeAsync();

        vm.SearchBoxText = "not-existing";

        Assert.False(vm.IsPaginationButtonVisible);
        Assert.False(vm.HasModelsToDisplay);
        Assert.Empty(vm.ModelItemViewModels);
    }

    [Fact]
    public async Task Sorting_Downloaded_ShouldShowDownloadedModelsFirst()
    {
        SetupMocks();

        var models = new List<OllamaModel>
        {
            new() { Name = "model-1", Size = 1_000_000_000, IsDownloaded = false },
            new() { Name = "model-2", Size = 2_000_000_000, IsDownloaded = true },
            new() { Name = "model-3", Size = 3_000_000_000, IsDownloaded = false },
            new() { Name = "model-4", Size = 4_000_000_000, IsDownloaded = true }
        };

        fixture.ModelCacheMock
            .Setup(o => o.GetCachedModelsAsync())
            .ReturnsAsync(models);

        var vm = CreateViewModel();

        await vm.InitializeAsync();

        vm.SelectedSortingOption = SortingOption.Downloaded;

        var ordered = vm.ModelItemViewModels.ToList();

        var downloadedSegment = ordered
            .TakeWhile(m => m.CurrentStatus?.DownloadState == DownloadState.Downloaded)
            .ToList();

        var restSegment = ordered.Skip(downloadedSegment.Count).ToList();

        Assert.NotEmpty(downloadedSegment);
        Assert.All(downloadedSegment, m => Assert.Equal(DownloadState.Downloaded, m.CurrentStatus?.DownloadState));
        Assert.All(restSegment, m => Assert.NotEqual(DownloadState.Downloaded, m.CurrentStatus?.DownloadState));
    }

    [Fact]
    public async Task Sorting_SizeAscending_ShouldOrderBySizeAsc()
    {
        SetupMocks();

        var models = new List<OllamaModel>
        {
            new() { Name = "model-1", Size = 3_000_000_000 },
            new() { Name = "model-2", Size = 1_000_000_000 },
            new() { Name = "model-3", Size = 2_000_000_000 }
        };

        fixture.ModelCacheMock
            .Setup(o => o.GetCachedModelsAsync())
            .ReturnsAsync(models);

        var vm = CreateViewModel();

        await vm.InitializeAsync();

        vm.SelectedSortingOption = SortingOption.SizeAscending;

        var sizes = vm.ModelItemViewModels.Select(m => m.Model.Size).ToList();
        var sortedSizes = sizes.OrderBy(s => s).ToList();

        Assert.Equal(sortedSizes, sizes);
        Assert.Equal(1_000_000_000, sizes[0]); // model-2
        Assert.Equal(3_000_000_000, sizes[2]); // model-1
    }

    [Fact]
    public async Task Sorting_SizeDescending_ShouldOrderBySizeDesc()
    {
        SetupMocks();

        var models = new List<OllamaModel>
        {
            new() { Name = "model-1", Size = 3_000_000_000 },
            new() { Name = "model-2", Size = 1_000_000_000 },
            new() { Name = "model-3", Size = 2_000_000_000 }
        };

        fixture.ModelCacheMock
            .Setup(o => o.GetCachedModelsAsync())
            .ReturnsAsync(models);

        var vm = CreateViewModel();

        await vm.InitializeAsync();

        vm.SelectedSortingOption = SortingOption.SizeDescending;

        var sizes = vm.ModelItemViewModels.Select(m => m.Model.Size).ToList();
        var sortedSizes = sizes.OrderByDescending(s => s).ToList();

        Assert.Equal(sortedSizes, sizes);
        Assert.Equal(3_000_000_000, sizes[0]);
    }

    [Fact]
    public async Task Sorting_PullCountAscending_ShouldOrderByPullCountAsc()
    {
        SetupMocks();

        var models = new List<OllamaModel>
        {
            new() { Name = "model-1", Family = new OllamaModelFamily { PullCount = 30 } },
            new() { Name = "model-2", Family = new OllamaModelFamily { PullCount = 10 } },
            new() { Name = "model-3", Family = new OllamaModelFamily { PullCount = 20 } }
        };

        fixture.ModelCacheMock
            .Setup(o => o.GetCachedModelsAsync())
            .ReturnsAsync(models);

        var vm = CreateViewModel();

        await vm.InitializeAsync();

        vm.SelectedSortingOption = SortingOption.PullCountAscending;

        var pulls = vm.ModelItemViewModels.Select(m => m.Model.Family?.PullCount).ToList();
        var sortedPulls = pulls.OrderBy(p => p).ToList();

        Assert.Equal(sortedPulls, pulls);
        Assert.Equal(10, pulls[0]);
    }

    [Fact]
    public async Task Sorting_PullCountDescending_ShouldOrderByPullCountDesc()
    {
        SetupMocks();

        var models = new List<OllamaModel>
        {
            new() { Name = "model-1", Family = new OllamaModelFamily { PullCount = 30 } },
            new() { Name = "model-2", Family = new OllamaModelFamily { PullCount = 10 } },
            new() { Name = "model-3", Family = new OllamaModelFamily { PullCount = 20 } }
        };

        fixture.ModelCacheMock
            .Setup(o => o.GetCachedModelsAsync())
            .ReturnsAsync(models);

        var vm = CreateViewModel();

        await vm.InitializeAsync();

        vm.SelectedSortingOption = SortingOption.PullCountDescending;

        var pulls = vm.ModelItemViewModels.Select(m => m.Model.Family?.PullCount).ToList();
        var sortedPulls = pulls.OrderByDescending(p => p).ToList();

        Assert.Equal(sortedPulls, pulls);
        Assert.Equal(30, pulls[0]);
    }
}
