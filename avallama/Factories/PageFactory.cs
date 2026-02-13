// Copyright (c) Márk Csörgő and Martin Bartos
// Licensed under the MIT License. See LICENSE file for details.

using System;
using avallama.Constants.Application;
using avallama.ViewModels;

namespace avallama.Factories;

public class PageFactory(Func<ApplicationPage, PageViewModel> factory)
{
    public PageViewModel GetPageViewModel(ApplicationPage page) => factory.Invoke(page);
}
