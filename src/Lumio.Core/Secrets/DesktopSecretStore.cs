namespace Lumio.Core.Secrets;

/// <summary>Picks the right secure store for the desktop OS the app is running on.</summary>
public static class DesktopSecretStore
{
    public static ISecretStore Create()
    {
        if (OperatingSystem.IsMacOS())
            return new MacKeychainSecretStore();
        if (OperatingSystem.IsWindows())
            return new WindowsCredentialSecretStore();
        if (OperatingSystem.IsLinux() && LinuxSecretToolStore.IsAvailable())
            return new LinuxSecretToolStore();
        return new InMemorySecretStore();
    }
}
