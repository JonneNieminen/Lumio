namespace Lumio.Core.Model;

/// <summary>The outcome class of a single connection check.</summary>
public enum CheckOutcome
{
    /// <summary>The API answered as expected.</summary>
    Healthy,

    /// <summary>The API answered, but something needs attention (slow, rate limited).</summary>
    Degraded,

    /// <summary>The API could not be reached or rejected the request.</summary>
    Failed,
}

/// <summary>The result of testing a connection once.</summary>
/// <param name="CheckedAt">When the check ran.</param>
/// <param name="Outcome">Healthy, degraded or failed.</param>
/// <param name="HttpStatus">The HTTP status code, when a response arrived.</param>
/// <param name="LatencyMs">Round-trip time in milliseconds.</param>
/// <param name="Summary">What happened, in plain words.</param>
/// <param name="Advice">What to do next, when something went wrong.</param>
public sealed record CheckResult(
    DateTimeOffset CheckedAt,
    CheckOutcome Outcome,
    int? HttpStatus,
    long LatencyMs,
    string Summary,
    string? Advice = null);
