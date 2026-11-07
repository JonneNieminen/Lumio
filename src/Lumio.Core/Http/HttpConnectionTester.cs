using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Lumio.Core.Model;

namespace Lumio.Core.Http;

/// <summary>
/// Tests a connection with one GET request to its health endpoint and turns the outcome into
/// plain-language guidance.
/// </summary>
public sealed class HttpConnectionTester(HttpClient http, TimeProvider? time = null) : IConnectionTester
{
    /// <summary>Responses slower than this count as "needs attention".</summary>
    public static readonly TimeSpan SlowThreshold = TimeSpan.FromMilliseconds(1500);

    private readonly TimeProvider _time = time ?? TimeProvider.System;

    public async Task<CheckResult> TestAsync(ConnectionDraft draft, CancellationToken ct = default)
    {
        var now = _time.GetUtcNow();
        if (!UrlUtil.IsHttpUrl(draft.BaseUrl))
            return new(now, CheckOutcome.Failed, null, 0,
                "The base URL isn't a valid web address.",
                "Use a full address that starts with https://");

        var endpoint = new Uri(draft.Endpoint);
        using var request = BuildRequest(endpoint, draft);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(Math.Clamp(draft.TimeoutSeconds, 1, 120)));

        var stopwatch = Stopwatch.StartNew();
        try
        {
            using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token)
                .ConfigureAwait(false);
            stopwatch.Stop();
            return Classify(now, (int)response.StatusCode, stopwatch.ElapsedMilliseconds);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            return new(now, CheckOutcome.Failed, null, stopwatch.ElapsedMilliseconds,
                $"No response within {draft.TimeoutSeconds} seconds.",
                "Check the base URL, or raise the timeout in the configure step.");
        }
        catch (HttpRequestException ex)
        {
            return new(now, CheckOutcome.Failed, null, stopwatch.ElapsedMilliseconds,
                $"Couldn't reach {endpoint.Host}.",
                ex.HttpRequestError switch
                {
                    HttpRequestError.NameResolutionError => "The address doesn't resolve. Check the base URL for typos.",
                    HttpRequestError.SecureConnectionError => "The secure connection failed. Check the certificate or use https.",
                    HttpRequestError.ConnectionError => "The server refused the connection. Check the address and port.",
                    _ => "Check the address and your network connection, then test again.",
                });
        }
    }

    internal static HttpRequestMessage BuildRequest(Uri endpoint, ConnectionDraft draft)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
        request.Headers.UserAgent.Add(new ProductInfoHeaderValue("Lumio", "0.1"));
        foreach (var (name, value) in draft.Headers)
            request.Headers.TryAddWithoutValidation(name, value);

        var secret = draft.Secret ?? string.Empty;
        switch (draft.AuthKind)
        {
            case AuthKind.Bearer:
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", secret);
                break;
            case AuthKind.ApiKeyHeader:
                request.Headers.TryAddWithoutValidation(
                    string.IsNullOrWhiteSpace(draft.ApiKeyHeader) ? "X-API-Key" : draft.ApiKeyHeader.Trim(), secret);
                break;
            case AuthKind.Basic:
                var raw = Encoding.UTF8.GetBytes($"{draft.Username}:{secret}");
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(raw));
                break;
        }
        return request;
    }

    internal static CheckResult Classify(DateTimeOffset at, int status, long latencyMs) => status switch
    {
        >= 200 and < 300 when latencyMs > SlowThreshold.TotalMilliseconds =>
            new(at, CheckOutcome.Degraded, status, latencyMs,
                $"Connected, but slow ({latencyMs} ms).",
                "The API answered. If it stays slow, check the provider's status page."),
        >= 200 and < 300 =>
            new(at, CheckOutcome.Healthy, status, latencyMs, $"Connected in {latencyMs} ms."),
        (int)HttpStatusCode.Unauthorized or (int)HttpStatusCode.Forbidden =>
            new(at, CheckOutcome.Failed, status, latencyMs,
                $"The API rejected the credentials ({status}).",
                "Check that the key is correct, not expired, and has read access."),
        (int)HttpStatusCode.NotFound =>
            new(at, CheckOutcome.Failed, status, latencyMs,
                "The health path wasn't found (404).",
                "Check the base URL and health path in the configure step."),
        (int)HttpStatusCode.TooManyRequests =>
            new(at, CheckOutcome.Degraded, status, latencyMs,
                "The API is rate limiting requests (429).",
                "Wait a minute, then test again."),
        >= 500 =>
            new(at, CheckOutcome.Failed, status, latencyMs,
                $"The provider returned a server error ({status}).",
                "The problem is on their side. Check the provider's status page and try later."),
        _ =>
            new(at, CheckOutcome.Failed, status, latencyMs,
                $"The API answered with status {status}.",
                "Check the health path and authentication settings."),
    };
}
