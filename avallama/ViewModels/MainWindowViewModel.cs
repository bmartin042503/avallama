namespace avallama.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    /*
     * Ugye itt ezek ki lesznek cserelve ha a nyelvi beallitasok jonnek, csak ide raktam oket
     */
    public string Greeting { get; } = "avallama";
    public string Version { get; } = "v0.1.0-alpha";
    public string Info { get; } = "Run any open source LLM locally";
}