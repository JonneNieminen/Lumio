using Lumio.Core.Http;
using Lumio.Core.Services;
using Lumio.Core.Storage;
using Lumio.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Lumio.Services;

public static class ServiceCollectionExtensions
{
    /// <summary>Registers Lumio's core services, app services and view models.</summary>
    /// <remarks>Platform heads add an <c>ISecretStore</c> through <see cref="App.ConfigurePlatformServices"/>.</remarks>
    public static IServiceCollection AddLumio(this IServiceCollection services)
    {
        // Core
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton(_ => new HttpClient { Timeout = Timeout.InfiniteTimeSpan });
        services.AddSingleton<IConnectionTester>(sp =>
            new HttpConnectionTester(sp.GetRequiredService<HttpClient>(), sp.GetRequiredService<TimeProvider>()));
        services.AddSingleton<IConnectionRepository>(_ => new JsonConnectionRepository(AppPaths.ConnectionsFile));
        services.AddSingleton(_ => new SettingsStore(AppPaths.SettingsFile));
        services.AddSingleton<ConnectionService>(sp => new ConnectionService(
            sp.GetRequiredService<IConnectionRepository>(),
            sp.GetRequiredService<Lumio.Core.Secrets.ISecretStore>(),
            sp.GetRequiredService<IConnectionTester>(),
            sp.GetRequiredService<TimeProvider>()));

        // App services
        services.AddSingleton<NavigationService>();
        services.AddSingleton<INavigationService>(sp => sp.GetRequiredService<NavigationService>());
        services.AddSingleton<ThemeService>();
        services.AddSingleton<IThemeService>(sp => sp.GetRequiredService<ThemeService>());
        services.AddSingleton<UrlLauncher>();
        services.AddSingleton<IUrlLauncher>(sp => sp.GetRequiredService<UrlLauncher>());
        services.AddSingleton<IViewModelFactory, ViewModelFactory>();

        // View models
        services.AddSingleton<ShellViewModel>();
        services.AddTransient<ConnectionsViewModel>();
        services.AddTransient<AddConnectionViewModel>();
        services.AddTransient<ConnectionDetailViewModel>();
        services.AddTransient<SettingsViewModel>();
        return services;
    }

    private sealed class ViewModelFactory(IServiceProvider services) : IViewModelFactory
    {
        public T Create<T>() where T : PageViewModel => services.GetRequiredService<T>();
    }
}
