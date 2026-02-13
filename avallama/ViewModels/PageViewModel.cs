// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using CommunityToolkit.Mvvm.ComponentModel;
using avallama.Constants.Application;

namespace avallama.ViewModels;
public partial class PageViewModel : ViewModelBase
{
    [ObservableProperty]
    private ApplicationPage _page;
}
