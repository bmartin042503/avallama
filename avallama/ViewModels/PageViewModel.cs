using CommunityToolkit.Mvvm.ComponentModel;
using avallama.Constants;

namespace avallama.ViewModels;
public partial class PageViewModel : ViewModelBase
{
    [ObservableProperty]
    private ApplicationPage _page;
}