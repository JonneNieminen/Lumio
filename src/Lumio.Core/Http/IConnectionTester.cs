using Lumio.Core.Model;

namespace Lumio.Core.Http;

/// <summary>Checks whether an API answers with the given settings and credentials.</summary>
public interface IConnectionTester
{
    Task<CheckResult> TestAsync(ConnectionDraft draft, CancellationToken ct = default);
}
