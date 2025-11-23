using System.Collections.Generic;
using avallama.Models;
using avallama.Services;

namespace avallama.Constants;

public static class GuideItems
{
    public static IList<GuideItem> GetGuideItems()
    {
        return new List<GuideItem>
        {
            new(
                title: LocalizationService.GetString("GUIDE_ITEM_1_TITLE"),
                description: LocalizationService.GetString("GUIDE_ITEM_1_DESCRIPTION"),
                imageSource: "avares://avallama/Assets/Images/home.png"
            ),
            new(
                title: LocalizationService.GetString("GUIDE_ITEM_2_TITLE"),
                description: LocalizationService.GetString("GUIDE_ITEM_2_DESCRIPTION"),
                imageSource: "avares://avallama/Assets/Images/modelmanager.png"
            ),
            new(
                title: LocalizationService.GetString("GUIDE_ITEM_3_TITLE"),
                description: LocalizationService.GetString("GUIDE_ITEM_3_DESCRIPTION"),
                imageSource: "avares://avallama/Assets/Images/settings.png"
            )
        };
    }
}
