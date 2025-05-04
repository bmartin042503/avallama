using System;
using System.Runtime.InteropServices;

namespace avallama;

// Osztály ami betölti a macOS-hez kapcsolódó natív könyvtárakat
// így például kezelhetőek natívan bizonyos elemek, melyet az Avalonia nem támogat
public static class MacOSInterop
{
    // ez egy saját, rövid Objective-C fájl amit xcodeban lebuildeltem és így használni tudja az Avalonia
    // csak annyi van benne hogy visszaad egy bool értéket hogy az aktív ablak fullscreenben van-e vagy sem
    // a WindowState.Maximized erre nem működik
    // macOS release csomagba mindenképp be kell tenni ezt a fájlt a többi dll közé különben kivételt kap
    // fejlesztésre meg hogy működjön bin/Debug/net9.0-ba
    
    [DllImport("libFullScreenCheck.dylib", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool isKeyWindowInFullScreen();
}