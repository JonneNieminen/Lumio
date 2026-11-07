using Avalonia.Animation;
using Avalonia.Controls;
using Lumio.ViewModels;
using Lumio.Views;

namespace Lumio.Services;

/// <summary>Maps page view models to pages and drives the shell's <see cref="NavigationPage"/>.</summary>
public sealed class NavigationService : INavigationService
{
    private static readonly IPageTransition SectionTransition = new CrossFade(TimeSpan.FromMilliseconds(200));
    private NavigationPage? _host;

    public void Attach(NavigationPage host) => _host = host;

    private NavigationPage Host => _host ?? throw new InvalidOperationException("Navigation host is not attached yet.");

    public async Task SetRootAsync(PageViewModel page)
    {
        var view = CreatePage(page);
        if (Host.StackDepth == 0)
        {
            await Host.PushAsync(view, SectionTransition);
            return;
        }
        if (Host.StackDepth > 1)
            await Host.PopToRootAsync();
        await Host.ReplaceAsync(view, SectionTransition);
    }

    public Task PushAsync(PageViewModel page) => Host.PushAsync(CreatePage(page));

    public Task ReplaceAsync(PageViewModel page) => Host.ReplaceAsync(CreatePage(page));

    public Task PopAsync() => Host.CanGoBack ? Host.PopAsync() : Task.CompletedTask;

    /// <summary>Explicit mapping keeps navigation trimming-safe (no reflection-based view locator).</summary>
    internal static Page CreatePage(PageViewModel vm) => vm switch
    {
        ConnectionsViewModel => new ConnectionsPage { DataContext = vm },
        AddConnectionViewModel => new AddConnectionPage { DataContext = vm },
        ConnectionDetailViewModel => new ConnectionDetailPage { DataContext = vm },
        SettingsViewModel => new SettingsPage { DataContext = vm },
        _ => throw new ArgumentOutOfRangeException(nameof(vm), vm.GetType().Name, "No page is registered for this view model."),
    };
}
