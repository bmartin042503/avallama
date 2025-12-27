using System;
using System.Runtime.InteropServices;

namespace avallama;

// Class that loads native libraries related to macOS
// thus certain elements can be handled natively that Avalonia does not support
public static class MacOSInterop
{
    // this is a short Objective-C file that I built in xcode so Avalonia can use it.
    // it only contains a method that returns a bool value indicating whether the active window is in fullscreen or not
    // WindowState.Maximized does not work for this
    // in macOS release package this file must be placed among the other dlls otherwise an exception will be thrown
    // for development to work it must be placed in bin/Debug/net9.0 and the quarantine flag must be removed with the command 'xattr -d com.apple.quarantine'

    [DllImport("libFullScreenCheck.dylib", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool isKeyWindowInFullScreen();
}
