using System.Globalization;

namespace avallama.Services;
public static class LocalizationService
{
    private static readonly System.Threading.Lock Lock = new();
    private static System.Resources.ResourceManager? _resourceMan;
    private static CultureInfo _resourceCulture = CultureInfo.CurrentUICulture;
    
    // ResourceManager példány lekérése
    private static System.Resources.ResourceManager ResourceManager {
        get {
            // szálbiztos inicializálás
            if (_resourceMan != null) return _resourceMan;
            lock (Lock)
            {
                _resourceMan = new System.Resources.ResourceManager("avallama.Assets.Localization.Resources",
                    typeof(LocalizationService).Assembly);
            }
            return _resourceMan;
        }
    }
    
    // CultureInfo beállítása, lekérése
    public static CultureInfo Culture
    {
        get => _resourceCulture;
        set
        {
            _resourceCulture = value;
            CultureInfo.CurrentUICulture = value;
        }
    }

    // Lokalizált szöveg lekérése kulcs alapján
    public static string GetString(string key)
    {
        return ResourceManager.GetString(key, _resourceCulture) ?? "undefined key";
    }
}