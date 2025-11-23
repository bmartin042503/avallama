using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using avallama.Models;
using avallama.Services;
using avallama.Tests.Fixtures;
using avallama.ViewModels;
using avallama.Views;
using Avalonia.Controls;
using Moq;
using Xunit;
using Avalonia.Headless.XUnit;

namespace avallama.Tests.ViewModels;

public class HomeViewModelTests(TestServicesFixture fixture) : IClassFixture<TestServicesFixture>
{
    private void DbMock()
    {
        // Mock db svc to prevent NullReferenceException when testing
        fixture.DbMock.Setup(db => db.GetConversations())
            .ReturnsAsync([]);
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
            .Setup(o => o.ListDownloadedModels())
            .ReturnsAsync(models);
        DbMock();

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

        await vm.InitializeModels(test: true);

        Assert.Equal(availableModels, vm.AvailableModels);
        Assert.Equal("model1", vm.SelectedModelName);
        Assert.True(vm.IsModelsDropdownEnabled);
    }

    [Fact]
    public async Task InitializeModels_WhenEmptyList_IsEmpty_SelectedEmptyString_DropdownDisabled()
    {
        fixture.OllamaMock.Reset();
        fixture.OllamaMock
            .Setup(o => o.ListDownloadedModels())
            .ReturnsAsync([]);
        DbMock();

        var vm = new HomeViewModel(
            fixture.OllamaMock.Object,
            fixture.DialogMock.Object,
            fixture.ConfigMock.Object,
            fixture.DbMock.Object,
            fixture.MessengerMock.Object
        );

        await vm.InitializeModels(test: true);

        Assert.Single(vm.AvailableModels);
        Assert.Equal(LocalizationService.GetString("NO_MODELS_FOUND"), vm.SelectedModelName);
        Assert.False(vm.IsModelsDropdownEnabled);
    }

    [AvaloniaFact]
    public async Task View_WhenEmptyModelsList_DisablesComboBox()
    {
        fixture.OllamaMock.Setup(o => o.ListDownloadedModels())
            .ReturnsAsync([]);
        DbMock();

        var vm = new HomeViewModel(
            fixture.OllamaMock.Object,
            fixture.DialogMock.Object,
            fixture.ConfigMock.Object,
            fixture.DbMock.Object,
            fixture.MessengerMock.Object);

        var view = new HomeView { DataContext = vm };

        await vm.InitializeModels(test: true);

        var combo = view.FindControl<ComboBox>("ModelsComboBox");
        var warning = view.FindControl<TextBlock>("NoModelsWarningTextBlock");

        Assert.NotNull(combo);
        Assert.NotNull(warning);
        Assert.False(combo.IsEnabled);
        Assert.False(combo.IsDropDownOpen);
        Assert.True(warning.IsVisible);
    }
}
