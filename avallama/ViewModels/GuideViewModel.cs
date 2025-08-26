// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using System.Collections.Generic;
using avallama.Constants;
using avallama.Models;
using avallama.Services;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace avallama.ViewModels;

public partial class GuideViewModel : PageViewModel
{
    [ObservableProperty] private string _currentTitle = string.Empty;
    [ObservableProperty] private string? _currentDescription;
    [ObservableProperty] private Bitmap? _currentImage;
    [ObservableProperty] private bool _isNextButtonEnabled = true;
    [ObservableProperty] private bool _isImageVisible = true;
    [ObservableProperty] private string _skipButtonText = LocalizationService.GetString("SKIP");

    private int _guideIndex;

    private readonly IList<GuideItem> _guideItems;

    public GuideViewModel()
    {
        _guideItems = GuideItems.GetGuideItems();
        CurrentTitle = _guideItems[0].Title;
        CurrentDescription = _guideItems[0].Description;
        CurrentImage = LoadGuideImage(_guideItems[0].ImageSource);
    }

    private static Bitmap LoadGuideImage(string imageSource)
    {
        var uri = new Uri(imageSource);
        return new Bitmap(AssetLoader.Open(uri));
    }

    [RelayCommand]
    public void Next()
    {
        if (_guideIndex == _guideItems.Count - 1)
        {
            CurrentTitle = LocalizationService.GetString("THANK_YOU");
            CurrentDescription = string.Empty;
            IsImageVisible = false;
            IsNextButtonEnabled = false;
            SkipButtonText = LocalizationService.GetString("START");
            return;
        }
        _guideIndex++;
        CurrentTitle = _guideItems[_guideIndex].Title;
        CurrentImage = LoadGuideImage(_guideItems[_guideIndex].ImageSource);
    }
}