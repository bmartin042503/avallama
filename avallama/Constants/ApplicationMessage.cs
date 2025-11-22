// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using CommunityToolkit.Mvvm.Messaging.Messages;

namespace avallama.Constants;


public static class ApplicationMessage
{
    // request ollama start confirmation dialog
    public class AskOllamaStart() : ValueChangedMessage<bool>(true);

    // request for app shutdown
    public class Shutdown() : ValueChangedMessage<bool>(true);

    // request for app restart
    public class Restart() : ValueChangedMessage<bool>(true);

    // request for settings to reload
    public class ReloadSettings() : ValueChangedMessage<bool>(true);

    // request for an application page
    public class RequestPage(ApplicationPage page) : ValueChangedMessage<bool>(true)
    {
        public ApplicationPage Page { get; } = page;
    }
}
