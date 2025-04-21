using System;
using System.Collections.Generic;
using avallama.Services;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace avallama.ViewModels;

public partial class GuideViewModel : PageViewModel
{
    [ObservableProperty] private string _currentMainText = string.Empty;
    [ObservableProperty] private string _currentSubText = string.Empty;
    [ObservableProperty] private Bitmap? _currentImageSource;
    [ObservableProperty] private bool _isNextButtonEnabled = true;
    [ObservableProperty] private bool _isImageVisible = true;

    private int _guideIndex;

    private readonly List<string> _guideMainTexts = [];
    private readonly List<string> _guideImageSources = [];
    // private readonly List<string> _guideMainSubTexts = [];

    public GuideViewModel()
    {
        GuideResourcesInit();
        CurrentMainText = _guideMainTexts[0];
        CurrentSubText = LocalizationService.GetString("COMING_SOON");
        CurrentImageSource = LoadGuideImage(_guideImageSources[0]);
    }

    private void GuideResourcesInit()
    {
        _guideMainTexts.Add(LocalizationService.GetString("GUIDE_1_MAIN_TEXT"));
        _guideMainTexts.Add(LocalizationService.GetString("GUIDE_2_MAIN_TEXT"));
        _guideMainTexts.Add(LocalizationService.GetString("GUIDE_3_MAIN_TEXT"));
        
        _guideImageSources.Add("avares://avallama/Assets/Images/home.png");
        _guideImageSources.Add("avares://avallama/Assets/Images/home.png");
        _guideImageSources.Add("avares://avallama/Assets/Images/settings.png");
    }

    private Bitmap LoadGuideImage(string imageSource)
    {
        var uri = new Uri(imageSource);
        return new Bitmap(AssetLoader.Open(uri));
    }

    [RelayCommand]
    public void Next()
    {
        if (_guideIndex == 2)
        {
            CurrentMainText = LocalizationService.GetString("THANK_YOU");
            CurrentSubText = string.Empty;
            IsImageVisible = false;
            IsNextButtonEnabled = false;
            return;
        }
        _guideIndex++;
        CurrentMainText = _guideMainTexts[_guideIndex];
        CurrentImageSource = LoadGuideImage(_guideImageSources[_guideIndex]);
    }
}