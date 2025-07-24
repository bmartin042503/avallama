// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using avallama.Constants;

namespace avallama.ViewModels;

public partial class GreetingViewModel : PageViewModel
{
    public GreetingViewModel()
    {
        // beállítás, hogy a viewmodel milyen paget kezel
        Page = ApplicationPage.Greeting;
    }
}