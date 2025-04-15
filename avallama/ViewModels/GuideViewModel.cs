using System.Collections.Generic;
using avallama.Services;
using CommunityToolkit.Mvvm.Input;

namespace avallama.ViewModels;

public partial class GuideViewModel : PageViewModel
{
    private int _guideIndex;
    private List<string> _guideMainTexts;
    private List<string> _guideMainSubTexts;

    public GuideViewModel()
    {
        _guideMainTexts = new List<string>();
        _guideMainSubTexts = new List<string>();
        
        _guideMainTexts.Add(LocalizationService.GetString("GUIDE_1_MAIN_TEXT"));
        _guideMainTexts.Add(LocalizationService.GetString("GUIDE_2_MAIN_TEXT"));
        _guideMainTexts.Add(LocalizationService.GetString("GUIDE_3_MAIN_TEXT"));
        
        _guideMainTexts.Add(LocalizationService.GetString("GUIDE_1_SUB_TEXT"));
    }

    [RelayCommand]
    public void Next()
    {
        _guideIndex++;
    }
}