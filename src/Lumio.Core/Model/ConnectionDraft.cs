namespace Lumio.Core.Model;

/// <summary>Everything needed to test or create a connection, including the secret.</summary>
public sealed record ConnectionDraft
{
    public required string ProviderId { get; init; }
    public required string Name { get; init; }
    public required string BaseUrl { get; init; }
    public string HealthPath { get; init; } = "/";
    public AuthKind AuthKind { get; init; }
    public string? ApiKeyHeader { get; init; }
    public string? Username { get; init; }
    public string? Secret { get; init; }
    public int TimeoutSeconds { get; init; } = 10;

    /// <summary>Extra request headers the provider needs (for example an API version).</summary>
    public IReadOnlyDictionary<string, string> Headers { get; init; } = new Dictionary<string, string>();

    public string Endpoint => UrlUtil.Combine(BaseUrl, HealthPath);
}
