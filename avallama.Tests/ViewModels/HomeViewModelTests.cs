// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using avallama.Models;
using avallama.Tests.Fixtures;
using avallama.ViewModels;
using Moq;
using Xunit;

namespace avallama.Tests.ViewModels;

public class HomeViewModelTests(TestServicesFixture fixture) : IClassFixture<TestServicesFixture>
{
    private void SetupMock()
    {
        // Mock db svc to prevent NullReferenceException when testing
        fixture.DbMock.Setup(db => db.GetConversations())
            .ReturnsAsync([]);

        fixture.OllamaMock
            .Setup(x => x.WaitForRunningAsync())
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task InitializeModels_WhenNonEmptyList_HasItems_SelectedLastModel_DropdownEnabled()
    {
        fixture.OllamaMock.Reset();
        var models = new List<OllamaModel>
        {
            new() { Name = "model1" },
            new() { Name = "model2" }
        };
        fixture.OllamaMock
            .Setup(o => o.GetDownloadedModels())
            .ReturnsAsync(models);

        SetupMock();

        var vm = new HomeViewModel(
            fixture.OllamaMock.Object,
            fixture.DialogMock.Object,
            fixture.ConfigMock.Object,
            fixture.DbMock.Object,
            fixture.MessengerMock.Object
        );

        var availableModels = new ObservableCollection<string>
        {
            "model1",
            "model2"
        };

        await vm.InitializeAsync();

        Assert.Equal(availableModels, vm.AvailableModels);
        Assert.Equal("model1", vm.SelectedModelName);
        Assert.True(vm.IsModelsDropdownEnabled);
    }

    [Fact]
    public async Task InitializeModels_WhenEmptyList_IsEmpty_SelectedEmptyString_DropdownDisabled()
    {
        fixture.OllamaMock.Reset();
        fixture.OllamaMock
            .Setup(o => o.GetDownloadedModels())
            .ReturnsAsync([]);

        SetupMock();

        var vm = new HomeViewModel(
            fixture.OllamaMock.Object,
            fixture.DialogMock.Object,
            fixture.ConfigMock.Object,
            fixture.DbMock.Object,
            fixture.MessengerMock.Object
        );

        await vm.InitializeAsync();

        Assert.Empty(vm.AvailableModels);
        Assert.False(vm.IsModelsDropdownEnabled);
    }
}
