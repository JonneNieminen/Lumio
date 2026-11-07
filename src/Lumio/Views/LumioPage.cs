using Avalonia.Controls;
using Lumio.ViewModels;

namespace Lumio.Views;

/// <summary>
/// Base page: forwards navigation lifecycle to the page's view model, so view models can
/// refresh when the page reappears after Back.
/// </summary>
public class LumioPage : ContentPage
{
    protected override Type StyleKeyOverride => typeof(ContentPage);

    protected override async void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);
        if (DataContext is PageViewModel vm)
            await vm.OnAppearingAsync();
    }
}
