using Lumio.Core.Model;
using Lumio.Core.Secrets;
using Lumio.Core.Services;
using Lumio.Services;
using Lumio.ViewModels;

namespace Lumio.Tests;

public class AddConnectionViewModelTests
{
    private sealed class Factory(ConnectionService svc, INavigationService nav) : IViewModelFactory
    {
        public T Create<T>() where T : PageViewModel => (T)(object)(typeof(T).Name switch
        {
            nameof(ConnectionDetailViewModel) => new ConnectionDetailViewModel(svc, nav, new NoopLauncher(), TimeProvider.System),
            _ => throw new NotSupportedException(typeof(T).Name),
        });
    }

    private static (AddConnectionViewModel Vm, RecordingNavigation Nav, MemoryRepository Repo, FakeTester Tester) Create()
    {
        var repo = new MemoryRepository();
        var tester = new FakeTester();
        var svc = new ConnectionService(repo, new InMemorySecretStore(), tester);
        var nav = new RecordingNavigation();
        return (new AddConnectionViewModel(svc, nav, new Factory(svc, nav), new NoopLauncher()), nav, repo, tester);
    }

    [Fact]
    public async Task Cannot_leave_first_step_without_a_provider()
    {
        var (vm, _, _, _) = Create();
        await vm.PrimaryCommand.ExecuteAsync(null);
        Assert.Equal(AddConnectionViewModel.ChooseStep, vm.CurrentStep);
        Assert.NotNull(vm.ProviderError);
    }

    [Fact]
    public void Choosing_a_provider_fills_defaults()
    {
        var (vm, _, _, _) = Create();
        vm.SelectedProvider = ProviderCatalog.Get("github");
        Assert.Equal("https://api.github.com", vm.BaseUrl);
        Assert.Equal("/user", vm.HealthPath);
        Assert.Equal("GitHub", vm.Name);
        Assert.True(vm.NeedsSecret);
    }

    [Fact]
    public void Edited_name_survives_a_provider_change()
    {
        var (vm, _, _, _) = Create();
        vm.SelectedProvider = ProviderCatalog.Get("github");
        vm.Name = "Work GitHub";
        vm.SelectedProvider = ProviderCatalog.Get("stripe");
        Assert.Equal("Work GitHub", vm.Name);
    }

    [Fact]
    public async Task Secret_is_required_for_authenticated_providers()
    {
        var (vm, _, _, _) = Create();
        vm.SelectedProvider = ProviderCatalog.Get("stripe");
        await vm.PrimaryCommand.ExecuteAsync(null);
        await vm.PrimaryCommand.ExecuteAsync(null);
        Assert.Equal(AddConnectionViewModel.AuthStep, vm.CurrentStep);
        Assert.Contains("secret key", vm.SecretError);
    }

    [Fact]
    public async Task Full_flow_tests_then_saves_and_shows_detail()
    {
        var (vm, nav, repo, tester) = Create();
        vm.SelectedProvider = ProviderCatalog.Get("github");
        await vm.PrimaryCommand.ExecuteAsync(null); // -> auth
        vm.Secret = "  ghp_abc  ";
        await vm.PrimaryCommand.ExecuteAsync(null); // -> configure
        await vm.PrimaryCommand.ExecuteAsync(null); // -> test (runs the check)

        Assert.Equal(AddConnectionViewModel.TestStep, vm.CurrentStep);
        Assert.True(vm.IsHealthy);
        var sent = Assert.Single(tester.Calls);
        Assert.Equal("ghp_abc", sent.Secret);
        Assert.Equal("2022-11-28", sent.Headers["X-GitHub-Api-Version"]);

        await vm.PrimaryCommand.ExecuteAsync(null); // save
        Assert.Single(repo.Saved);
        Assert.Equal("replace:ConnectionDetailViewModel", nav.Log.Last());
    }

    [Fact]
    public async Task Back_from_first_step_cancels()
    {
        var (vm, nav, _, _) = Create();
        await vm.BackCommand.ExecuteAsync(null);
        Assert.Equal("pop", nav.Log.Single());
    }

    [Fact]
    public void Public_api_needs_no_secret()
    {
        var (vm, _, _, _) = Create();
        vm.SelectedProvider = ProviderCatalog.Get("jsonplaceholder");
        Assert.True(vm.IsPublic);
        Assert.Equal("None needed", vm.SecretSummary);
    }
}

public class FormattingTests
{
    [Fact]
    public void Ago_reads_naturally()
    {
        var now = new DateTimeOffset(2026, 11, 7, 12, 0, 0, TimeSpan.Zero);
        Assert.Equal("just now", Formatting.Ago(now.AddSeconds(-20), now));
        Assert.Equal("5 min ago", Formatting.Ago(now.AddMinutes(-5), now));
        Assert.Equal("3 h ago", Formatting.Ago(now.AddHours(-3), now));
        Assert.Equal("2 d ago", Formatting.Ago(now.AddDays(-2), now));
    }
}
