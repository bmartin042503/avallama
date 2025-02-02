using avallama.Constants;
using avallama.Factories;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
namespace avallama.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    // PageFactory amivel elérhető az App.axaml.cs-ben létrehozott delegate, vagyis adott PageViewModel visszaadása
    private readonly PageFactory _pageFactory;
    
    [ObservableProperty]
    private PageViewModel _currentPageViewModel;

    public MainViewModel(PageFactory pageFactory)
    {
        _pageFactory = pageFactory;
        CurrentPageViewModel = _pageFactory.GetPageViewModel(ApplicationPage.Greeting);
    }

    [RelayCommand]
    private void GoToHome()
    {
        CurrentPageViewModel = _pageFactory.GetPageViewModel(ApplicationPage.Home);
    }
}