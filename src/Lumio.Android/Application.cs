using Android.App;
using Android.Runtime;
using Avalonia;
using Avalonia.Android;
using Avalonia.Media;
using Lumio.Core.Secrets;
using Microsoft.Extensions.DependencyInjection;

namespace Lumio.Android;

[Application]
public class Application : AvaloniaAndroidApplication<App>
{
    protected Application(nint javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
    {
    }

    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        App.ConfigurePlatformServices = services =>
            services.AddSingleton<ISecretStore>(new AndroidKeystoreSecretStore(this));

        return base.CustomizeAppBuilder(builder)
            .WithInterFont()
            .With(new FontManagerOptions { DefaultFamilyName = "fonts:Inter#Inter" });
    }
}
