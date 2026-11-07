using Lumio.Core.Model;

namespace Lumio.Core.Storage;

/// <summary>Persists connection metadata (never secrets).</summary>
public interface IConnectionRepository
{
    Task<IReadOnlyList<Connection>> LoadAllAsync(CancellationToken ct = default);
    Task SaveAllAsync(IReadOnlyList<Connection> connections, CancellationToken ct = default);
}
