using Lumio.Core.Http;
using Lumio.Core.Model;
using Lumio.Core.Secrets;
using Lumio.Core.Storage;

namespace Lumio.Core.Services;

/// <summary>
/// The one place that coordinates connection metadata, secrets and health checks.
/// View models talk to this, never to the repository or secret store directly.
/// </summary>
public sealed class ConnectionService(
    IConnectionRepository repository,
    ISecretStore secrets,
    IConnectionTester tester,
    TimeProvider? time = null)
{
    private readonly TimeProvider _time = time ?? TimeProvider.System;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private List<Connection>? _cache;

    public ISecretStore SecretStore => secrets;

    public async Task<IReadOnlyList<Connection>> GetAllAsync(CancellationToken ct = default)
    {
        var all = await SnapshotAsync(ct).ConfigureAwait(false);
        return [.. all.OrderBy(c => c.Name, StringComparer.CurrentCultureIgnoreCase)];
    }

    public async Task<Connection?> GetAsync(Guid id, CancellationToken ct = default) =>
        (await SnapshotAsync(ct).ConfigureAwait(false)).FirstOrDefault(c => c.Id == id);

    /// <summary>Tests a draft that hasn't been saved yet.</summary>
    public Task<CheckResult> TestDraftAsync(ConnectionDraft draft, CancellationToken ct = default) =>
        tester.TestAsync(draft, ct);

    /// <summary>Saves a new connection. The secret goes to the secure store first.</summary>
    public async Task<Connection> CreateAsync(ConnectionDraft draft, CheckResult? firstCheck, CancellationToken ct = default)
    {
        var connection = new Connection
        {
            Id = Guid.NewGuid(),
            ProviderId = draft.ProviderId,
            Name = draft.Name.Trim(),
            BaseUrl = draft.BaseUrl.Trim().TrimEnd('/'),
            HealthPath = string.IsNullOrWhiteSpace(draft.HealthPath) ? "/" : draft.HealthPath.Trim(),
            AuthKind = draft.AuthKind,
            ApiKeyHeader = draft.AuthKind == AuthKind.ApiKeyHeader ? draft.ApiKeyHeader : null,
            Username = draft.AuthKind == AuthKind.Basic ? draft.Username : null,
            TimeoutSeconds = draft.TimeoutSeconds,
            CreatedAt = _time.GetUtcNow(),
            History = firstCheck is null ? [] : [firstCheck],
        };

        if (draft.AuthKind != AuthKind.None && !string.IsNullOrEmpty(draft.Secret))
            await secrets.SetAsync(connection.SecretKey, draft.Secret, ct).ConfigureAwait(false);

        await MutateAsync(list => list.Add(connection), ct).ConfigureAwait(false);
        return connection;
    }

    /// <summary>Runs a health check on a saved connection and records the result.</summary>
    public async Task<CheckResult> TestAsync(Guid id, CancellationToken ct = default)
    {
        var connection = await GetAsync(id, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException("That connection no longer exists.");

        CheckResult result;
        string? secret = null;
        if (connection.AuthKind != AuthKind.None)
        {
            try
            {
                secret = await secrets.GetAsync(connection.SecretKey, ct).ConfigureAwait(false);
            }
            catch (SecretStoreException ex)
            {
                result = new(_time.GetUtcNow(), CheckOutcome.Failed, null, 0, ex.Message,
                    "Allow Lumio to use the keychain item, then test again.");
                await RecordAsync(id, result, ct).ConfigureAwait(false);
                return result;
            }
        }

        if (connection.AuthKind != AuthKind.None && string.IsNullOrEmpty(secret))
        {
            result = new(_time.GetUtcNow(), CheckOutcome.Failed, null, 0,
                "The saved secret is missing from the secure store.",
                "Remove this connection and add it again with a fresh key.");
        }
        else
        {
            result = await tester.TestAsync(ToDraft(connection, secret), ct).ConfigureAwait(false);
        }

        await RecordAsync(id, result, ct).ConfigureAwait(false);
        return result;
    }

    public async Task RenameAsync(Guid id, string name, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            return;
        await MutateAsync(list =>
        {
            var i = list.FindIndex(c => c.Id == id);
            if (i >= 0)
                list[i] = list[i] with { Name = name.Trim() };
        }, ct).ConfigureAwait(false);
    }

    /// <summary>Removes a connection and its secret.</summary>
    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        await secrets.DeleteAsync(Connection.SecretKeyFor(id), ct).ConfigureAwait(false);
        await MutateAsync(list => list.RemoveAll(c => c.Id == id), ct).ConfigureAwait(false);
    }

    public static ConnectionDraft ToDraft(Connection c, string? secret) => new()
    {
        ProviderId = c.ProviderId,
        Name = c.Name,
        BaseUrl = c.BaseUrl,
        HealthPath = c.HealthPath,
        AuthKind = c.AuthKind,
        ApiKeyHeader = c.ApiKeyHeader,
        Username = c.Username,
        Secret = secret,
        TimeoutSeconds = c.TimeoutSeconds,
        Headers = ProviderCatalog.Get(c.ProviderId).Headers,
    };

    private Task RecordAsync(Guid id, CheckResult result, CancellationToken ct) =>
        MutateAsync(list =>
        {
            var i = list.FindIndex(c => c.Id == id);
            if (i >= 0)
                list[i] = list[i].WithCheck(result);
        }, ct);

    private async Task<List<Connection>> LoadAsync(CancellationToken ct)
    {
        if (_cache is not null)
            return _cache;
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            return _cache ??= [.. await repository.LoadAllAsync(ct).ConfigureAwait(false)];
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>A copy of the cached list, taken under the lock so concurrent writes can't tear it.</summary>
    private async Task<List<Connection>> SnapshotAsync(CancellationToken ct)
    {
        await LoadAsync(ct).ConfigureAwait(false);
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            return [.. _cache!];
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task MutateAsync(Action<List<Connection>> change, CancellationToken ct)
    {
        await LoadAsync(ct).ConfigureAwait(false);
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            change(_cache!);
            await repository.SaveAllAsync(_cache!, ct).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }
}
