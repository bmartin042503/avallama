using avallama.Services;

namespace avallama.ViewModels;

public partial class HomeViewModel : ViewModelBase
{
    public string LanguageLimitationWarning { get; } = LocalizationService.GetString("ONLY_SUPPORTED_MODEL");
    public string ResourceLimitWarning { get; } = "This model may not run optimally because you have 6.9GB of VRAM, " +
                                                  "and this model is 420GB"; //Ez hardkodolva, mert ugyis moge jon majd az uzleti logika
}