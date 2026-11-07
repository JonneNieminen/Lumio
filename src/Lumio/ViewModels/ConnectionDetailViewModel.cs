using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lumio.Core.Model;
using Lumio.Core.Services;
using Lumio.Services;

namespace Lumio.ViewModels;

/// <summary>One connection: health, details, recent checks and removal.</summary>
public sealed partial class ConnectionDetailViewModel(
    ConnectionService connections,
    INavigationService navigation,
    IUrlLauncher launcher,
    TimeProvider time) : PageViewModel
{
    private Connection? _connection;

    public Guid ConnectionId { get; set; }

    public override string Title => "Connection details";

    [ObservableProperty] public partial bool IsLoaded { get; private set; }
    [ObservableProperty] public partial bool IsMissing { get; private set; }
    [ObservableProperty] public partial string Name { get; private set; } = "";
    [ObservableProperty] public partial string ProviderName { get; private set; } = "";
    [ObservableProperty] public partial string Monogram { get; private set; } = "";
    [ObservableProperty] public partial string Endpoint { get; private set; } = "";
    [ObservableProperty] public partial string AuthText { get; private set; } = "";
    [ObservableProperty] public partial string SecretStorageText { get; private set; } = "";
    [ObservableProperty] public partial string TimeoutText { get; private set; } = "";
    [ObservableProperty] public partial string CreatedText { get; private set; } = "";
    [ObservableProperty] public partial ConnectionStatus Status { get; private set; }
    [ObservableProperty] public partial string StatusText { get; private set; } = "";

    [ObservableProperty] public partial string LastCheckText { get; private set; } = "—";
    [ObservableProperty] public partial string LatencyText { get; private set; } = "—";
    [ObservableProperty] public partial string HealthyText { get; private set; } = "—";
    [ObservableProperty] public partial string HealthyCaption { get; private set; } = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasLastCheck), nameof(LastIsHealthy), nameof(LastIsDegraded), nameof(LastIsFailed),
        nameof(LastSummary), nameof(LastAdvice), nameof(HasAdvice))]
    public partial CheckResult? LastCheck { get; private set; }

    public bool HasLastCheck => LastCheck is not null;
    public bool LastIsHealthy => LastCheck?.Outcome == CheckOutcome.Healthy;
    public bool LastIsDegraded => LastCheck?.Outcome == CheckOutcome.Degraded;
    public bool LastIsFailed => LastCheck?.Outcome == CheckOutcome.Failed;
    public string LastSummary => LastCheck?.Summary ?? "";
    public string LastAdvice => LastCheck?.Advice ?? "";
    public bool HasAdvice => !string.IsNullOrEmpty(LastCheck?.Advice);

    public ObservableCollection<HistoryRowViewModel> History { get; } = [];
    public bool HasHistory => History.Count > 0;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(TestCommand))]
    public partial bool IsTesting { get; private set; }

    [ObservableProperty] public partial bool IsConfirmingRemove { get; private set; }
    [ObservableProperty] public partial string? ErrorText { get; private set; }

    public bool HasDocs => _connection is not null && ProviderCatalog.Get(_connection.ProviderId).DocsUrl is not null;

    public override Task OnAppearingAsync() => LoadAsync();

    public async Task LoadAsync()
    {
        var c = await connections.GetAsync(ConnectionId);
        IsMissing = c is null;
        if (c is not null)
            Apply(c);
        IsLoaded = true;
    }

    private bool CanTest() => !IsTesting;

    [RelayCommand(CanExecute = nameof(CanTest))]
    private async Task TestAsync()
    {
        IsTesting = true;
        ErrorText = null;
        try
        {
            await connections.TestAsync(ConnectionId);
            await LoadAsync();
        }
        catch (InvalidOperationException ex)
        {
            ErrorText = ex.Message;
        }
        finally
        {
            IsTesting = false;
        }
    }

    [RelayCommand]
    private void AskRemove() => IsConfirmingRemove = true;

    [RelayCommand]
    private void CancelRemove() => IsConfirmingRemove = false;

    [RelayCommand]
    private async Task ConfirmRemoveAsync()
    {
        try
        {
            await connections.DeleteAsync(ConnectionId);
            await navigation.PopAsync();
        }
        catch (Exception ex) when (ex is Lumio.Core.Secrets.SecretStoreException or IOException)
        {
            ErrorText = $"Lumio couldn't remove the connection. {ex.Message}";
            IsConfirmingRemove = false;
        }
    }

    [RelayCommand]
    private Task OpenDocsAsync() =>
        _connection is not null && ProviderCatalog.Get(_connection.ProviderId).DocsUrl is { } url
            ? launcher.OpenAsync(url)
            : Task.CompletedTask;

    private void Apply(Connection c)
    {
        _connection = c;
        var now = time.GetUtcNow();
        var provider = ProviderCatalog.Get(c.ProviderId);

        Name = c.Name;
        ProviderName = provider.Name;
        Monogram = provider.Monogram;
        Endpoint = $"GET {c.Endpoint}";
        AuthText = Formatting.AuthText(c.AuthKind, c.ApiKeyHeader)
                   + (c.AuthKind == AuthKind.Basic && c.Username is { } u ? $" as {u}" : "");
        SecretStorageText = c.AuthKind == AuthKind.None ? "No secret needed" : connections.SecretStore.DisplayName;
        TimeoutText = $"{c.TimeoutSeconds} seconds";
        CreatedText = c.CreatedAt.ToLocalTime().ToString("d MMM yyyy, HH:mm");
        Status = c.Status;
        StatusText = Formatting.StatusText(c.Status);
        LastCheck = c.LastCheck;

        LastCheckText = c.LastCheck is { } last ? Formatting.Ago(last.CheckedAt, now) : "Never";
        var answered = c.History.Where(h => h.HttpStatus is not null).ToList();
        LatencyText = answered.Count > 0 ? $"{(long)answered.Average(h => h.LatencyMs)} ms" : "—";
        var healthy = c.History.Count(h => h.Outcome == CheckOutcome.Healthy);
        HealthyText = c.History.Count > 0 ? $"{healthy}/{c.History.Count}" : "—";
        HealthyCaption = c.History.Count switch
        {
            0 => "No checks yet",
            1 => "From the only check so far",
            var n => $"Across the last {n} checks",
        };

        History.Clear();
        foreach (var h in c.History)
            History.Add(new HistoryRowViewModel(h, now));

        OnPropertyChanged(nameof(HasHistory));
        OnPropertyChanged(nameof(HasDocs));
    }
}

/// <summary>One row in a connection's recent checks.</summary>
public sealed class HistoryRowViewModel(CheckResult result, DateTimeOffset now)
{
    public ConnectionStatus Status { get; } = Formatting.ToStatus(result.Outcome);
    public string OutcomeText { get; } = Formatting.OutcomeText(result.Outcome);
    public string Summary { get; } = result.Summary;
    public string When { get; } = Formatting.Ago(result.CheckedAt, now);
    public string Code { get; } = result.HttpStatus?.ToString() ?? "—";
    public string Latency { get; } = Formatting.Latency(result.LatencyMs);
}
