namespace avallama.ViewModels;
using avallama.Assets.Localization;

public partial class MainWindowViewModel : ViewModelBase
{
    /*
     * Ugye itt ezek ki lesznek cserelve ha a nyelvi beallitasok jonnek, csak ide raktam oket
     */
    public string Greeting { get; } = Resources.ResourceManager.GetString("Greeting_Text")!;
    public string Version { get; } = "v0.1.0-alpha";
    public string Info { get; } = Resources.ResourceManager.GetString("Greeting_Subtext")!;
}