using CommunityToolkit.Mvvm.ComponentModel;

namespace Lumio.ViewModels;

public abstract class ViewModelBase : ObservableObject;

/// <summary>A view model shown as a page in the navigation stack.</summary>
public abstract class PageViewModel : ViewModelBase
{
    public abstract string Title { get; }

    /// <summary>Called each time the page becomes the visible page (including on Back).</summary>
    public virtual Task OnAppearingAsync() => Task.CompletedTask;
}
