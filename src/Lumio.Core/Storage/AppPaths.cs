namespace Lumio.Core.Storage;

/// <summary>Where Lumio keeps its local files.</summary>
public static class AppPaths
{
    /// <summary>
    /// The per-user data folder: <c>~/Library/Application Support/Lumio</c> on macOS,
    /// <c>%LOCALAPPDATA%\Lumio</c> on Windows, <c>~/.local/share/Lumio</c> on Linux and
    /// the app's private files folder on Android.
    /// </summary>
    public static string DataDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.Create),
        "Lumio");

    public static string ConnectionsFile => Path.Combine(DataDirectory, "connections.json");
    public static string SettingsFile => Path.Combine(DataDirectory, "settings.json");
}
