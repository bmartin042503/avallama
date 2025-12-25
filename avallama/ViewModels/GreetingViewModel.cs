// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using avallama.Constants;

namespace avallama.ViewModels;

public partial class GreetingViewModel : PageViewModel
{
    public GreetingViewModel()
    {
        // setting the page type that this viewmodel handles
        Page = ApplicationPage.Greeting;
    }
}
