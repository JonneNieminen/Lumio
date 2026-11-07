namespace Lumio.Core.Model;

/// <summary>How Lumio authenticates against an API.</summary>
public enum AuthKind
{
    /// <summary>No credentials (public APIs).</summary>
    None,

    /// <summary><c>Authorization: Bearer &lt;secret&gt;</c>.</summary>
    Bearer,

    /// <summary>The secret goes in a named request header, for example <c>X-API-Key</c>.</summary>
    ApiKeyHeader,

    /// <summary>HTTP Basic: a username plus the secret as the password.</summary>
    Basic,
}
