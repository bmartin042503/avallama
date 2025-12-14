// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using Avalonia;
using avallama.Tests.Fixtures;
using Avalonia.Headless;
using Avalonia.Media;

[assembly: AvaloniaTestApplication(typeof(TestAppBuilder))]

namespace avallama.Tests.Fixtures;

public class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>()
        .UseSkia()
        .UseHeadless(new AvaloniaHeadlessPlatformOptions
        {
            UseHeadlessDrawing = false
        });
}
