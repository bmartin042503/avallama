using System;
using avallama.Constants;
using Avalonia.Data;
using CommunityToolkit.Mvvm.Input;

namespace avallama.ViewModels;
using avallama.Services;

public partial class GreetingViewModel : PageViewModel
{
    public GreetingViewModel()
    {
        // beállítás, hogy a viewmodel milyen paget kezel
        Page = ApplicationPage.Greeting;
    }
}