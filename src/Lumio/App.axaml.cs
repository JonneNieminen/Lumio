using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Lumio.Core.Secrets;
using Lumio.Core.Storage;
using Lumio.Services;
using Lumio.ViewModels;
using Lumio.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lumio;

public partial class App : Application
{
    /// <summary>
    /// Set by each platform head before startup to register platform services, most importantly
    /// the <see cref="ISecretStore"/> (keychain, Credential Manager, Secret Service, Android Keystore).
    /// </summary>
    public static Action<IServiceCollection>? ConfigurePlatformServices { get; set; }

    public static IServiceProvider Services { get; private set; } = null!;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection().AddLumio();
        ConfigurePlatformServices?.Invoke(services);
        services.TryAddSingleton<ISecretStore, InMemorySecretStore>();
        Services = services.BuildServiceProvider();

        var settings = Services.GetRequiredService<SettingsStore>().Load();
        var theme = Services.GetRequiredService<ThemeService>();
        theme.ApplyTheme(settings.Theme);
        theme.ApplyReduceMotion(settings.ReduceMotion);

        var shellView = CreateShell();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow { Content = shellView };
        }
        else if (ApplicationLifetime is IActivityApplicationLifetime activity)
        {
            activity.MainViewFactory = () => new PageNavigationHost { Page = shellView };
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime single)
        {
            single.MainView = new PageNavigationHost { Page = shellView };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static MainView CreateShell()
    {
        var shell = Services.GetRequiredService<ShellViewModel>();
        var view = new MainView { DataContext = shell };
        Services.GetRequiredService<NavigationService>().Attach(view.RootNavigation);
        Services.GetRequiredService<ThemeService>().Attach(view);
        view.AttachedToVisualTree += (_, _) =>
            Services.GetRequiredService<UrlLauncher>().Attach(TopLevel.GetTopLevel(view));
        view.Loaded += async (_, _) => await shell.StartAsync();
        return view;
    }
}
