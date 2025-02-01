using System;
using Avalonia.Data;
using CommunityToolkit.Mvvm.Input;

namespace avallama.ViewModels;
using avallama.Services;

public partial class GreetingViewModel : ViewModelBase
{
    public string Greeting { get; } = LocalizationService.GetString("GREETING_TEXT");
    public string Version { get; } = "v0.1.0-alpha";
    public string Info { get; } = LocalizationService.GetString("GREETING_SUBTEXT");
    public string GetStarted { get; } = LocalizationService.GetString("GET_STARTED");
    
    
}