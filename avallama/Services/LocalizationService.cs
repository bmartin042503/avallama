using System.Globalization;

namespace avallama.Services;

// DI esetén konstruktor kell
public class LocalizationService
{
    private static global::System.Resources.ResourceManager _resourceMan;
    private static global::System.Globalization.CultureInfo _resourceCulture = CultureInfo.CurrentUICulture;
    
    // ResourceManager példány lekérése
    private static global::System.Resources.ResourceManager ResourceManager {
        get {
            if (object.ReferenceEquals(_resourceMan, null)) {
                global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("avallama.Assets.Localization.Resources", typeof(LocalizationService).Assembly);
                _resourceMan = temp;
            }
            return _resourceMan;
        }
    }
    
    // CultureInfo beállítása, lekérése
    public static global::System.Globalization.CultureInfo Culture
    {
        get => _resourceCulture;
        set => _resourceCulture = value;
    }

    // Lokalizált szöveg lekérése kulcs alapján
    public static string GetString(string key)
    {
        return ResourceManager.GetString(key, _resourceCulture) ?? "undefined";
    }
}