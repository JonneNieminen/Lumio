using System.Text.Json;
using System.Text.Json.Serialization;

namespace Lumio.Core.Storage;

public enum ThemeChoice
{
    System,
    Dark,
    Light,
}

/// <summary>User preferences.</summary>
public sealed record AppSettings
{
    public ThemeChoice Theme { get; init; } = ThemeChoice.Dark;
    public bool ReduceMotion { get; init; }
}

/// <summary>Loads and saves <see cref="AppSettings"/> as JSON.</summary>
public sealed class SettingsStore(string filePath)
{
    public AppSettings Load()
    {
        try
        {
            if (File.Exists(filePath))
                return JsonSerializer.Deserialize(File.ReadAllText(filePath), SettingsJsonContext.Default.AppSettings)
                    ?? new AppSettings();
        }
        catch (JsonException)
        {
            // A corrupt settings file falls back to defaults rather than blocking startup.
        }
        return new AppSettings();
    }

    public void Save(AppSettings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        File.WriteAllText(filePath, JsonSerializer.Serialize(settings, SettingsJsonContext.Default.AppSettings));
    }
}

[JsonSourceGenerationOptions(WriteIndented = true, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UseStringEnumConverter = true)]
[JsonSerializable(typeof(AppSettings))]
internal sealed partial class SettingsJsonContext : JsonSerializerContext;
