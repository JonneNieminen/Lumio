using Lumio.Core.Model;

namespace Lumio.ViewModels;

/// <summary>Plain-language text for times, statuses and auth kinds.</summary>
public static class Formatting
{
    public static string Ago(DateTimeOffset then, DateTimeOffset now)
    {
        var span = now - then;
        if (span < TimeSpan.FromMinutes(1))
            return "just now";
        if (span < TimeSpan.FromHours(1))
            return $"{(int)span.TotalMinutes} min ago";
        if (span < TimeSpan.FromDays(1))
            return $"{(int)span.TotalHours} h ago";
        if (span < TimeSpan.FromDays(7))
            return $"{(int)span.TotalDays} d ago";
        return then.ToLocalTime().ToString("d MMM yyyy");
    }

    public static string StatusText(ConnectionStatus status) => status switch
    {
        ConnectionStatus.Connected => "Connected",
        ConnectionStatus.NeedsAttention => "Needs attention",
        ConnectionStatus.Failed => "Failed",
        _ => "Not tested",
    };

    public static string OutcomeText(CheckOutcome outcome) => outcome switch
    {
        CheckOutcome.Healthy => "Healthy",
        CheckOutcome.Degraded => "Degraded",
        _ => "Failed",
    };

    public static ConnectionStatus ToStatus(CheckOutcome outcome) => outcome switch
    {
        CheckOutcome.Healthy => ConnectionStatus.Connected,
        CheckOutcome.Degraded => ConnectionStatus.NeedsAttention,
        _ => ConnectionStatus.Failed,
    };

    public static string AuthText(AuthKind kind, string? header = null) => kind switch
    {
        AuthKind.None => "None (public API)",
        AuthKind.Bearer => "Bearer token",
        AuthKind.ApiKeyHeader => $"API key in {(string.IsNullOrWhiteSpace(header) ? "X-API-Key" : header)} header",
        AuthKind.Basic => "Basic (username and password)",
        _ => kind.ToString(),
    };

    public static string Latency(long ms) => ms > 0 ? $"{ms} ms" : "—";
}
