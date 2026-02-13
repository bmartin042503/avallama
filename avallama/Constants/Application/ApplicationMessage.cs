// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

namespace avallama.Constants.Application;

public static class ApplicationMessage
{
    // request ollama start confirmation dialog
    public record AskOllamaStart;

    // request for app shutdown
    public record Shutdown;

    // request for app restart
    public record Restart;

    // request for settings to reload
    public record ReloadSettings;

    // request for an application page
    public record RequestPage(ApplicationPage Page);

    // notification for ModelManagerViewModel when a Model status' changes
    public record ModelStatusChanged(string ModelName);
}
