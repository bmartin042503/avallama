using CommunityToolkit.Mvvm.Messaging.Messages;

namespace avallama.Constants;

// üzenetek/kérések, amiket az alkalmazás az MVVM rétegein belül küldhetnek egymásnak
public static class ApplicationMessage
{
    // kérés, hogy az app jelenítse meg az ollama indítási confirmation dialogot
    public class AskOllamaStart() : ValueChangedMessage<bool>(true);
    
    // kérés az alkalmazás leállítására
    public class Shutdown() : ValueChangedMessage<bool>(true);
    
    // kérés az alkalmazás újraindítására
    public class Restart() : ValueChangedMessage<bool>(true);
    
    // kérés a beállítások újratöltésére
    public class ReloadSettings() : ValueChangedMessage<bool>(true);
}