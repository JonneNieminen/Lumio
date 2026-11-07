using System.Collections.Concurrent;

namespace Lumio.Core.Secrets;

/// <summary>Keeps secrets in memory only. Used by tests and as a last-resort fallback.</summary>
public sealed class InMemorySecretStore : ISecretStore
{
    private readonly ConcurrentDictionary<string, string> _secrets = new();

    public string DisplayName => "Memory only (not saved)";
    public bool IsPersistent => false;

    public Task<string?> GetAsync(string key, CancellationToken ct = default) =>
        Task.FromResult(_secrets.TryGetValue(key, out var s) ? s : null);

    public Task SetAsync(string key, string secret, CancellationToken ct = default)
    {
        _secrets[key] = secret;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string key, CancellationToken ct = default)
    {
        _secrets.TryRemove(key, out _);
        return Task.CompletedTask;
    }
}
