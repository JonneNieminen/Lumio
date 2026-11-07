using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lumio.Core.Model;
using Lumio.Core.Services;
using Lumio.Services;

namespace Lumio.ViewModels;

/// <summary>The dashboard: every connection with its health at a glance.</summary>
public sealed partial class ConnectionsViewModel(
    ConnectionService connections,
    INavigationService navigation,
    IViewModelFactory factory,
    TimeProvider time) : PageViewModel
{
    public override string Title => "Connections";

    public ObservableCollection<ConnectionCardViewModel> Cards { get; } = [];

    [ObservableProperty] public partial bool IsLoaded { get; private set; }
    [ObservableProperty] public partial int ConnectedCount { get; private set; }
    [ObservableProperty] public partial int AttentionCount { get; private set; }
    [ObservableProperty] public partial int FailedCount { get; private set; }
    [ObservableProperty] public partial int UntestedCount { get; private set; }

    public bool IsEmpty => IsLoaded && Cards.Count == 0;
    public bool HasConnections => Cards.Count > 0;

    public string Subtitle => Cards.Count switch
    {
        0 => "Connect an API to watch its health here.",
        1 => "1 connection. Open it to see recent checks.",
        var n => $"{n} connections. Open one to see recent checks.",
    };

    public override Task OnAppearingAsync() => LoadAsync();

    public async Task LoadAsync()
    {
        var all = await connections.GetAllAsync();
        Cards.Clear();
        foreach (var c in all)
            Cards.Add(new ConnectionCardViewModel(c, time));
        IsLoaded = true;
        RefreshSummary();
    }

    [RelayCommand]
    private Task AddConnectionAsync() => navigation.PushAsync(factory.Create<AddConnectionViewModel>());

    [RelayCommand]
    private Task OpenAsync(ConnectionCardViewModel card)
    {
        var detail = factory.Create<ConnectionDetailViewModel>();
        detail.ConnectionId = card.Id;
        return navigation.PushAsync(detail);
    }

    /// <summary>Tests every connection, a few at a time.</summary>
    [RelayCommand]
    private async Task TestAllAsync(CancellationToken ct)
    {
        foreach (var card in Cards)
            card.IsTesting = true;

        await Parallel.ForEachAsync(Cards.ToList(), new ParallelOptions { MaxDegreeOfParallelism = 4, CancellationToken = ct },
            async (card, token) => await connections.TestAsync(card.Id, token));

        var all = await connections.GetAllAsync(ct);
        foreach (var card in Cards)
        {
            if (all.FirstOrDefault(c => c.Id == card.Id) is { } updated)
                card.Update(updated);
            card.IsTesting = false;
        }
        RefreshSummary();
    }

    private void RefreshSummary()
    {
        ConnectedCount = Cards.Count(c => c.Status == ConnectionStatus.Connected);
        AttentionCount = Cards.Count(c => c.Status == ConnectionStatus.NeedsAttention);
        FailedCount = Cards.Count(c => c.Status == ConnectionStatus.Failed);
        UntestedCount = Cards.Count(c => c.Status == ConnectionStatus.Untested);
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(HasConnections));
        OnPropertyChanged(nameof(Subtitle));
    }
}
