namespace avallama.ViewModels;
using avallama.Services;

public partial class MainWindowViewModel : ViewModelBase
{
    public string Greeting { get; } = LocalizationService.GetString("Greeting_Text");
    public string Version { get; } = "v0.1.0-alpha";
    public string Info { get; } = LocalizationService.GetString("Greeting_Subtext");
    public string GetStarted { get; } = LocalizationService.GetString("Get_Started");
}