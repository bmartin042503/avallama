// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using avallama.Services;
using avallama.Tests.Fixtures;
using avallama.ViewModels;
using avallama.Views;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Moq;
using Xunit;

namespace avallama.Tests.Views;

public class HomeViewTests : IClassFixture<TestServicesFixture>
{
    private readonly TestServicesFixture _fixture;

    public HomeViewTests(TestServicesFixture fixture)
    {
        _fixture = fixture;

        _fixture.OllamaMock
            .Setup(x => x.OllamaServiceState)
            .Returns(new ServiceState(ServiceStatus.Stopped));

        _fixture.ConfigMock
            .Setup(x => x.ReadSetting(It.IsAny<string>()))
            .Returns("");
    }

    [AvaloniaFact]
    public void ClickingSideBarButton_TogglesSideBarCorrectly()
    {
        var viewModel = new HomeViewModel(
            _fixture.OllamaMock.Object,
            _fixture.DialogMock.Object,
            _fixture.ConfigMock.Object,
            _fixture.DbMock.Object,
            _fixture.MessengerMock.Object
        );

        var view = new HomeView
        {
            DataContext = viewModel
        };

        var window = new Window { Content = view };
        window.Show();

        var sideBarBtn = view.FindControl<Button>("SideBarButton");
        var sideBar = view.FindControl<Grid>("SideBar");
        var mainGrid = view.FindControl<Grid>("MainGrid");

        Assert.NotNull(sideBarBtn);
        Assert.NotNull(sideBar);
        Assert.NotNull(mainGrid);

        // check if sidebar is opened by default when window is shown
        Assert.Contains(sideBar, mainGrid.Children);
        Assert.True(mainGrid.ColumnDefinitions[0].Width.Value > 0); // checking sidebar's width

        // simulate a click on sidebar button
        sideBarBtn.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

        // check if sidebar is closed after clicking the sidebar button
        Assert.DoesNotContain(sideBar, mainGrid.Children);
        Assert.Equal(0, mainGrid.ColumnDefinitions[0].Width.Value);

        // simulate another click on sidebar button
        sideBarBtn.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

        // check if sidebar is opened after clicking the sidebar button
        Assert.Contains(sideBar, mainGrid.Children);
        Assert.True(mainGrid.ColumnDefinitions[0].Width.Value > 0);
    }
}
