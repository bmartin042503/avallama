using System;
using System.Windows.Input;
using Avalonia.Rendering.Composition;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
namespace avallama.ViewModels;


public partial class MainWindowViewModel : ViewModelBase
{
    
    [ObservableProperty]
    private ViewModelBase _currentViewModel;

    private readonly HomeViewModel _homeViewModel = new();
    private readonly GreetingViewModel _greetingViewModel = new();

    public MainWindowViewModel()
    {
        CurrentViewModel = _greetingViewModel; // *cries*
    }

    [RelayCommand]
    private void GoToHome()
    {
        CurrentViewModel = _homeViewModel;
    }
}