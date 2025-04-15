// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System.Diagnostics;
using System.Runtime.InteropServices;
using avallama.Constants;
using CommunityToolkit.Mvvm.Input;

namespace avallama.ViewModels;

public partial class GreetingViewModel : PageViewModel
{
    public GreetingViewModel()
    {
        // beállítás, hogy a viewmodel milyen paget kezel
        Page = ApplicationPage.Greeting;
    }
}