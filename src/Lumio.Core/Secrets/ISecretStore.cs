namespace Lumio.Core.Secrets;

/// <summary>Stores secrets (API keys, tokens, passwords) in the platform's secure store.</summary>
public interface ISecretStore
{
    /// <summary>A short name for the backing store, shown in settings (for example "macOS Keychain").</summary>
    string DisplayName { get; }

    /// <summary>False when secrets do not survive a restart.</summary>
    bool IsPersistent { get; }

    Task<string?> GetAsync(string key, CancellationToken ct = default);
    Task SetAsync(string key, string secret, CancellationToken ct = default);
    Task DeleteAsync(string key, CancellationToken ct = default);
}

/// <summary>Thrown when the secure store refuses or cannot complete an operation.</summary>
public sealed class SecretStoreException(string message, Exception? inner = null) : Exception(message, inner);

/// <summary>The service name Lumio registers its secrets under.</summary>
public static class SecretStoreDefaults
{
    public const string Service = "app.lumio";
}
