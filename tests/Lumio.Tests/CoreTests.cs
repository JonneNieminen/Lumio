using System.Net;
using Lumio.Core.Http;
using Lumio.Core.Model;
using Lumio.Core.Secrets;
using Lumio.Core.Services;
using Lumio.Core.Storage;

namespace Lumio.Tests;

public class UrlUtilTests
{
    [Theory]
    [InlineData("https://api.x.com", "/health", "https://api.x.com/health")]
    [InlineData("https://api.x.com/", "health", "https://api.x.com/health")]
    [InlineData("https://api.x.com//", "//v1/me", "https://api.x.com/v1/me")]
    [InlineData("https://api.x.com", "", "https://api.x.com/")]
    public void Combine_joins_with_one_slash(string b, string p, string expected) =>
        Assert.Equal(expected, UrlUtil.Combine(b, p));

    [Theory]
    [InlineData("https://api.github.com", true)]
    [InlineData("http://localhost:8080", true)]
    [InlineData("api.github.com", false)]
    [InlineData("ftp://files.example.com", false)]
    [InlineData("", false)]
    public void IsHttpUrl(string url, bool expected) => Assert.Equal(expected, UrlUtil.IsHttpUrl(url));
}

public class HttpConnectionTesterTests
{
    private static ConnectionDraft Draft(AuthKind auth = AuthKind.Bearer, string? secret = "s3cret") => new()
    {
        ProviderId = "custom",
        Name = "Test",
        BaseUrl = "https://api.example.com",
        HealthPath = "/health",
        AuthKind = auth,
        Secret = secret,
        Username = "ada",
        ApiKeyHeader = "X-Key",
    };

    [Fact]
    public async Task Healthy_on_2xx()
    {
        var tester = new HttpConnectionTester(new HttpClient(StubHandler.Status(HttpStatusCode.OK)));
        var result = await tester.TestAsync(Draft());
        Assert.Equal(CheckOutcome.Healthy, result.Outcome);
        Assert.Equal(200, result.HttpStatus);
        Assert.Null(result.Advice);
    }

    [Theory]
    [InlineData(401, CheckOutcome.Failed, "rejected the credentials")]
    [InlineData(403, CheckOutcome.Failed, "rejected the credentials")]
    [InlineData(404, CheckOutcome.Failed, "health path")]
    [InlineData(429, CheckOutcome.Degraded, "rate limiting")]
    [InlineData(503, CheckOutcome.Failed, "server error")]
    public void Classifies_status_codes_with_guidance(int status, CheckOutcome outcome, string phrase)
    {
        var result = HttpConnectionTester.Classify(DateTimeOffset.UtcNow, status, 80);
        Assert.Equal(outcome, result.Outcome);
        Assert.Contains(phrase, result.Summary);
        Assert.False(string.IsNullOrEmpty(result.Advice));
    }

    [Fact]
    public void Slow_success_needs_attention()
    {
        var result = HttpConnectionTester.Classify(DateTimeOffset.UtcNow, 200, 2400);
        Assert.Equal(CheckOutcome.Degraded, result.Outcome);
    }

    [Fact]
    public async Task Invalid_base_url_fails_without_a_request()
    {
        var handler = StubHandler.Status(HttpStatusCode.OK);
        var tester = new HttpConnectionTester(new HttpClient(handler));
        var result = await tester.TestAsync(Draft() with { BaseUrl = "not a url" });
        Assert.Equal(CheckOutcome.Failed, result.Outcome);
        Assert.Null(handler.LastRequest);
    }

    [Fact]
    public async Task Network_failure_says_what_to_do()
    {
        var handler = new StubHandler(_ => throw new HttpRequestException(HttpRequestError.NameResolutionError));
        var result = await new HttpConnectionTester(new HttpClient(handler)).TestAsync(Draft());
        Assert.Equal(CheckOutcome.Failed, result.Outcome);
        Assert.Contains("api.example.com", result.Summary);
        Assert.Contains("typos", result.Advice);
    }

    [Fact]
    public void Applies_each_auth_kind()
    {
        var uri = new Uri("https://api.example.com/health");

        using var bearer = HttpConnectionTester.BuildRequest(uri, Draft(AuthKind.Bearer));
        Assert.Equal("Bearer s3cret", bearer.Headers.Authorization?.ToString());

        using var header = HttpConnectionTester.BuildRequest(uri, Draft(AuthKind.ApiKeyHeader));
        Assert.Equal("s3cret", header.Headers.GetValues("X-Key").Single());
        Assert.Null(header.Headers.Authorization);

        using var basic = HttpConnectionTester.BuildRequest(uri, Draft(AuthKind.Basic));
        Assert.Equal("Basic " + Convert.ToBase64String("ada:s3cret"u8.ToArray()), basic.Headers.Authorization?.ToString());

        using var none = HttpConnectionTester.BuildRequest(uri, Draft(AuthKind.None));
        Assert.Null(none.Headers.Authorization);
    }
}

public class ConnectionServiceTests
{
    private static ConnectionDraft Draft(string secret = "tok") => new()
    {
        ProviderId = "github",
        Name = "  My GitHub ",
        BaseUrl = "https://api.github.com/",
        HealthPath = "/user",
        AuthKind = AuthKind.Bearer,
        Secret = secret,
    };

    [Fact]
    public async Task Create_puts_secret_in_store_and_never_in_metadata()
    {
        var repo = new MemoryRepository();
        var secrets = new InMemorySecretStore();
        var svc = new ConnectionService(repo, secrets, new FakeTester());

        var created = await svc.CreateAsync(Draft("ghp_secret"), null);

        Assert.Equal("ghp_secret", await secrets.GetAsync(created.SecretKey));
        Assert.Equal("My GitHub", created.Name);
        Assert.Equal("https://api.github.com", created.BaseUrl);
        Assert.Single(repo.Saved);
        Assert.Equal(ConnectionStatus.Untested, created.Status);
    }

    [Fact]
    public async Task Test_uses_stored_secret_and_records_history()
    {
        var tester = new FakeTester();
        var svc = new ConnectionService(new MemoryRepository(), new InMemorySecretStore(), tester);
        var created = await svc.CreateAsync(Draft("from-store"), null);

        await svc.TestAsync(created.Id);
        await svc.TestAsync(created.Id);

        Assert.All(tester.Calls, c => Assert.Equal("from-store", c.Secret));
        var reloaded = await svc.GetAsync(created.Id);
        Assert.Equal(2, reloaded!.History.Count);
        Assert.Equal(ConnectionStatus.Connected, reloaded.Status);
    }

    [Fact]
    public async Task Missing_secret_fails_with_guidance_instead_of_sending()
    {
        var secrets = new InMemorySecretStore();
        var tester = new FakeTester();
        var svc = new ConnectionService(new MemoryRepository(), secrets, tester);
        var created = await svc.CreateAsync(Draft(), null);
        await secrets.DeleteAsync(created.SecretKey);

        var result = await svc.TestAsync(created.Id);

        Assert.Equal(CheckOutcome.Failed, result.Outcome);
        Assert.Empty(tester.Calls);
    }

    [Fact]
    public async Task Delete_removes_secret_and_record()
    {
        var repo = new MemoryRepository();
        var secrets = new InMemorySecretStore();
        var svc = new ConnectionService(repo, secrets, new FakeTester());
        var created = await svc.CreateAsync(Draft(), null);

        await svc.DeleteAsync(created.Id);

        Assert.Null(await secrets.GetAsync(created.SecretKey));
        Assert.Empty(repo.Saved);
    }

    [Fact]
    public void History_is_capped_newest_first()
    {
        var c = new Connection { Id = Guid.NewGuid(), ProviderId = "custom", Name = "x", BaseUrl = "https://x" };
        for (var i = 0; i < Connection.MaxHistory + 5; i++)
            c = c.WithCheck(new CheckResult(DateTimeOffset.UnixEpoch.AddMinutes(i), CheckOutcome.Healthy, 200, i, "ok"));

        Assert.Equal(Connection.MaxHistory, c.History.Count);
        Assert.Equal(Connection.MaxHistory + 4, c.LastCheck!.LatencyMs);
    }
}

public class JsonConnectionRepositoryTests
{
    [Fact]
    public async Task Round_trips_and_writes_atomically()
    {
        var dir = Directory.CreateTempSubdirectory("lumio-tests");
        try
        {
            var path = Path.Combine(dir.FullName, "connections.json");
            var repo = new JsonConnectionRepository(path);
            var c = new Connection
            {
                Id = Guid.NewGuid(), ProviderId = "stripe", Name = "Stripe", BaseUrl = "https://api.stripe.com",
                AuthKind = AuthKind.Bearer, CreatedAt = DateTimeOffset.UnixEpoch,
            }.WithCheck(new CheckResult(DateTimeOffset.UnixEpoch, CheckOutcome.Degraded, 429, 10, "Rate limited", "Wait"));

            await repo.SaveAllAsync([c]);
            var loaded = await new JsonConnectionRepository(path).LoadAllAsync();

            var back = Assert.Single(loaded);
            Assert.Equal(c.Id, back.Id);
            Assert.Equal(ConnectionStatus.NeedsAttention, back.Status);
            Assert.False(File.Exists(path + ".tmp"));
            Assert.Contains("\"authKind\": \"Bearer\"", await File.ReadAllTextAsync(path));
        }
        finally
        {
            dir.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task Missing_file_is_empty()
    {
        var repo = new JsonConnectionRepository(Path.Combine(Path.GetTempPath(), $"none-{Guid.NewGuid():N}.json"));
        Assert.Empty(await repo.LoadAllAsync());
    }
}
