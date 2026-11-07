using CommunityToolkit.Mvvm.ComponentModel;
using Lumio.Core.Secrets;
using Lumio.Services;

namespace Lumio.ViewModels;

/// <summary>The app shell: drawer sections and app-wide status.</summary>
public sealed partial class ShellViewModel(
    INavigationService navigation,
    IViewModelFactory factory,
    ISecretStore secretStore) : ViewModelBase
{
    public const int ConnectionsSection = 0;
    public const int SettingsSection = 1;

    [ObservableProperty]
    public partial int SelectedSectionIndex { get; set; } = -1;

    public string SecretStoreText => secretStore.IsPersistent
        ? $"Secrets in {secretStore.DisplayName}"
        : "Secrets kept in memory only";

    public bool IsSecretStorePersistent => secretStore.IsPersistent;

    public Task StartAsync()
    {
        SelectedSectionIndex = ConnectionsSection;
        return Task.CompletedTask;
    }

    partial void OnSelectedSectionIndexChanged(int value)
    {
        PageViewModel? page = value switch
        {
            ConnectionsSection => factory.Create<ConnectionsViewModel>(),
            SettingsSection => factory.Create<SettingsViewModel>(),
            _ => null,
        };
        if (page is not null)
            _ = navigation.SetRootAsync(page);
    }
}
