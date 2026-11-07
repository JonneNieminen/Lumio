using Lumio.ViewModels;

namespace Lumio.Services;

/// <summary>View-model-first navigation over the app's <c>NavigationPage</c> stack.</summary>
public interface INavigationService
{
    /// <summary>Clears the stack and shows <paramref name="page"/> as the new root (drawer sections).</summary>
    Task SetRootAsync(PageViewModel page);

    Task PushAsync(PageViewModel page);

    /// <summary>Replaces the current page, so Back skips it (wizard finished, show the result).</summary>
    Task ReplaceAsync(PageViewModel page);

    Task PopAsync();
}

/// <summary>Creates page view models with their dependencies.</summary>
public interface IViewModelFactory
{
    T Create<T>() where T : PageViewModel;
}

/// <summary>Applies appearance preferences to the running app.</summary>
public interface IThemeService
{
    void ApplyTheme(Lumio.Core.Storage.ThemeChoice theme);
    void ApplyReduceMotion(bool reduce);
}

/// <summary>Opens links in the system browser.</summary>
public interface IUrlLauncher
{
    Task OpenAsync(string url);
}
