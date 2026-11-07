using CommunityToolkit.Mvvm.ComponentModel;
using Lumio.Core.Model;

namespace Lumio.ViewModels;

/// <summary>One integration card on the dashboard.</summary>
public sealed partial class ConnectionCardViewModel : ViewModelBase
{
    private readonly TimeProvider _time;

    public ConnectionCardViewModel(Connection connection, TimeProvider time)
    {
        _time = time;
        Id = connection.Id;
        Update(connection);
    }

    public Guid Id { get; }

    [ObservableProperty] public partial string Name { get; private set; } = "";
    [ObservableProperty] public partial string ProviderName { get; private set; } = "";
    [ObservableProperty] public partial string Monogram { get; private set; } = "";
    [ObservableProperty] public partial string Endpoint { get; private set; } = "";
    [ObservableProperty] public partial ConnectionStatus Status { get; private set; }
    [ObservableProperty] public partial string StatusText { get; private set; } = "";
    [ObservableProperty] public partial string LastActivityText { get; private set; } = "";
    [ObservableProperty] public partial bool IsTesting { get; set; }

    public void Update(Connection c)
    {
        var provider = ProviderCatalog.Get(c.ProviderId);
        Name = c.Name;
        ProviderName = provider.Name;
        Monogram = provider.Monogram;
        Endpoint = c.Endpoint;
        Status = c.Status;
        StatusText = Formatting.StatusText(c.Status);
        LastActivityText = c.LastCheck is { } last
            ? $"Checked {Formatting.Ago(last.CheckedAt, _time.GetUtcNow())}"
            : "Not tested yet";
    }
}
