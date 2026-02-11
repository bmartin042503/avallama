// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using avallama.Constants.Application;

namespace avallama.ViewModels;

public partial class GreetingViewModel : PageViewModel
{
    public string AppVersion => App.Version;
    public GreetingViewModel()
    {
        // setting the page type that this viewmodel handles
        Page = ApplicationPage.Greeting;
    }
}
