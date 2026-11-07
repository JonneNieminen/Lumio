namespace Lumio.Core.Model;

public static class UrlUtil
{
    /// <summary>Joins a base URL and a path with exactly one slash between them.</summary>
    public static string Combine(string baseUrl, string? path)
    {
        var b = (baseUrl ?? string.Empty).Trim().TrimEnd('/');
        var p = (path ?? string.Empty).Trim();
        if (p.Length == 0 || p == "/")
            return b + "/";
        return b + "/" + p.TrimStart('/');
    }

    /// <summary>True when <paramref name="url"/> is an absolute http or https URL.</summary>
    public static bool IsHttpUrl(string? url) =>
        Uri.TryCreate(url?.Trim(), UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp)
        && !string.IsNullOrEmpty(uri.Host);
}
