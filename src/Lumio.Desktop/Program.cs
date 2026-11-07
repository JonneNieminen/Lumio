using Avalonia;
using Avalonia.Media;
using Lumio.Core.Secrets;
using Microsoft.Extensions.DependencyInjection;

namespace Lumio.Desktop;

internal sealed class Program
{
    // Don't use Avalonia, third-party APIs or SynchronizationContext-reliant code before AppMain is called.
    [STAThread]
    public static void Main(string[] args)
    {
        App.ConfigurePlatformServices = services =>
            services.AddSingleton<ISecretStore>(DesktopSecretStore.Create());

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration; also used by the visual designer and the DevTools previewer.
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
