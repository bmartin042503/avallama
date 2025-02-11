using System;
using System.Globalization;
using Avalonia.Markup.Xaml;

namespace avallama.Services;
public class LocalizationService : MarkupExtension
{
    private static readonly System.Threading.Lock Lock = new();
    private static System.Resources.ResourceManager? _resourceMan;
    private static readonly CultureInfo ResourceCulture = CultureInfo.CurrentUICulture;
    
    // ResourceManager példány lekérése
    private static System.Resources.ResourceManager ResourceManager {
        get {
            // szálbiztos inicializálás
            if (_resourceMan != null) return _resourceMan;
            lock (Lock)
            {
                _resourceMan ??= new System.Resources.ResourceManager("avallama.Assets.Localization.Resources",
                    typeof(LocalizationService).Assembly);
            }
            return _resourceMan;
        }
    }

    // Lokalizált szöveg lekérése kulcs alapján
    public static string GetString(string key)
    {
        // nagy, izmos undefined szöveg xd csak hogy feltűnjön ha valamelyik ui elemre rossz a kulcs
        return ResourceManager.GetString(key, ResourceCulture) ?? "[UNDEFINED_LOCALIZATION_KEY]";
    }

    // MarkupExtension
    public string Key { get; set; } = string.Empty;
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        return GetString(Key);
    }
}