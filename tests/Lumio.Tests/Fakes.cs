using System.Net;
using Lumio.Core.Http;
using Lumio.Core.Model;
using Lumio.Core.Storage;
using Lumio.Services;
using Lumio.ViewModels;

namespace Lumio.Tests;

internal sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
{
    public HttpRequestMessage? LastRequest { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        LastRequest = request;
        return Task.FromResult(respond(request));
    }

    public static StubHandler Status(HttpStatusCode code) => new(_ => new HttpResponseMessage(code));
}

internal sealed class MemoryRepository : IConnectionRepository
{
    public List<Connection> Saved { get; private set; } = [];
    public int SaveCount { get; private set; }

    public Task<IReadOnlyList<Connection>> LoadAllAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Connection>>(Saved.ToList());

    public Task SaveAllAsync(IReadOnlyList<Connection> connections, CancellationToken ct = default)
    {
        Saved = [.. connections];
        SaveCount++;
        return Task.CompletedTask;
    }
}

internal sealed class FakeTester(CheckOutcome outcome = CheckOutcome.Healthy) : IConnectionTester
{
    public List<ConnectionDraft> Calls { get; } = [];

    public Task<CheckResult> TestAsync(ConnectionDraft draft, CancellationToken ct = default)
    {
        Calls.Add(draft);
        return Task.FromResult(new CheckResult(DateTimeOffset.UtcNow, outcome, 200, 42, "Connected in 42 ms."));
    }
}

internal sealed class RecordingNavigation : INavigationService
{
    public List<string> Log { get; } = [];
    public PageViewModel? Current { get; private set; }

    public Task SetRootAsync(PageViewModel page) { Log.Add($"root:{page.GetType().Name}"); Current = page; return Task.CompletedTask; }
    public Task PushAsync(PageViewModel page) { Log.Add($"push:{page.GetType().Name}"); Current = page; return Task.CompletedTask; }
    public Task ReplaceAsync(PageViewModel page) { Log.Add($"replace:{page.GetType().Name}"); Current = page; return Task.CompletedTask; }
    public Task PopAsync() { Log.Add("pop"); return Task.CompletedTask; }
}

internal sealed class NoopLauncher : IUrlLauncher
{
    public Task OpenAsync(string url) => Task.CompletedTask;
}
