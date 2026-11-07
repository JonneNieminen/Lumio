using Avalonia;
using Avalonia.Media;
using Lumio.Core.Secrets;
using Microsoft.Extensions.DependencyInjection;
using Velopack;

namespace Lumio.Desktop;

internal sealed class Program
{
    // Don't use Avalonia, third-party APIs or SynchronizationContext-reliant code before AppMain is called.
    [STAThread]
    public static void Main(string[] args)
    {
        // Must run first: during install, update and uninstall Velopack starts the app with hook
        // arguments, and Run() handles them and exits. No-op when not installed (dotnet run, IDE).
        // Checking for and applying updates comes later, behind an app-owned IAppUpdateService.
        VelopackApp.Build().Run();

        App.ConfigurePlatformServices = services =>
            services.AddSingleton<ISecretStore>(DesktopSecretStore.Create());

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration; also used by the visual designer and the DevTools previewer.
    // Keep Velopack out of here: the previewer calls this method.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .With(new FontManagerOptions { DefaultFamilyName = "fonts:Inter#Inter" })
            .With(new MacOSPlatformOptions { ShowInDock = true })
#if DEBUG
            .WithDeveloperTools()
#endif
            .LogToTrace();
}
