namespace Lumio.Core.Model;

/// <summary>A known API that Lumio can connect to, with sensible defaults.</summary>
public sealed record Provider
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }

    /// <summary>One or two letters shown in the provider tile.</summary>
    public required string Monogram { get; init; }

    public string BaseUrl { get; init; } = "";
    public string HealthPath { get; init; } = "/";
    public AuthKind AuthKind { get; init; } = AuthKind.Bearer;

    /// <summary>When true, the user picks the auth kind and edits the base URL.</summary>
    public bool IsCustom { get; init; }

    /// <summary>Label for the secret field, for example "Personal access token".</summary>
    public string SecretLabel { get; init; } = "API key";

    /// <summary>Hint for the secret format, shown as the placeholder.</summary>
    public string SecretPlaceholder { get; init; } = "";

    /// <summary>One line on where to find or how to scope the credential.</summary>
    public string SecretHelp { get; init; } = "";

    public string? DocsUrl { get; init; }

    public IReadOnlyDictionary<string, string> Headers { get; init; } = new Dictionary<string, string>();
}
