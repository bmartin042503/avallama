using System;
using avallama.Constants;
using avallama.ViewModels;

namespace avallama.Factories;

public class PageFactory(Func<ApplicationPage, PageViewModel> factory)
{
    public PageViewModel GetPageViewModel(ApplicationPage page) => factory.Invoke(page);
}