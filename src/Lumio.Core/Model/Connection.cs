using System.Text.Json.Serialization;

namespace Lumio.Core.Model;

/// <summary>
/// A saved API connection. Holds metadata only: the secret lives in the platform
/// secret store under <see cref="SecretKey"/>.
/// </summary>
public sealed record Connection
{
    /// <summary>How many past checks are kept per connection.</summary>
    public const int MaxHistory = 20;

    public required Guid Id { get; init; }
    public required string ProviderId { get; init; }
    public required string Name { get; init; }
    public required string BaseUrl { get; init; }
    public string HealthPath { get; init; } = "/";
    public AuthKind AuthKind { get; init; }
    public string? ApiKeyHeader { get; init; }
    public string? Username { get; init; }
    public int TimeoutSeconds { get; init; } = 10;
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>Past checks, newest first.</summary>
    public IReadOnlyList<CheckResult> History { get; init; } = [];

    [JsonIgnore]
    public CheckResult? LastCheck => History.Count > 0 ? History[0] : null;

    [JsonIgnore]
    public ConnectionStatus Status => LastCheck?.Outcome switch
    {
        null => ConnectionStatus.Untested,
        CheckOutcome.Healthy => ConnectionStatus.Connected,
        CheckOutcome.Degraded => ConnectionStatus.NeedsAttention,
        _ => ConnectionStatus.Failed,
    };

    [JsonIgnore]
    public string SecretKey => SecretKeyFor(Id);

    [JsonIgnore]
    public string Endpoint => UrlUtil.Combine(BaseUrl, HealthPath);

    public static string SecretKeyFor(Guid id) => $"connection.{id:N}";

    /// <summary>Returns a copy with <paramref name="result"/> prepended to the history.</summary>
    public Connection WithCheck(CheckResult result) =>
        this with { History = [result, .. History.Take(MaxHistory - 1)] };
}
