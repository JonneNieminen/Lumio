using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using Lumio.Core.Secrets;
using Lumio.Core.Storage;
using Lumio.Services;

namespace Lumio.ViewModels;

/// <summary>Appearance, security and app information.</summary>
public sealed partial class SettingsViewModel : PageViewModel
{
    private readonly IThemeService _theme;
    private readonly SettingsStore _store;
    private readonly bool _loading;

    public SettingsViewModel(IThemeService theme, SettingsStore store, ISecretStore secrets)
    {
        _theme = theme;
        _store = store;
        _loading = true;
        var settings = store.Load();
        Theme = settings.Theme;
        ReduceMotion = settings.ReduceMotion;
        _loading = false;

        SecretStoreName = secrets.DisplayName;
        SecretStoreNote = secrets.IsPersistent
            ? "API keys and tokens are encrypted by your operating system and never written to Lumio's own files."
            : "No system keychain was found, so secrets last until Lumio closes. Install a Secret Service provider to keep them.";
    }

    public override string Title => "Settings";

    [ObservableProperty] public partial ThemeChoice Theme { get; set; }
    [ObservableProperty] public partial bool ReduceMotion { get; set; }

    public string SecretStoreName { get; }
    public string SecretStoreNote { get; }
    public string DataFolder => AppPaths.DataDirectory;

    public string Version { get; } =
        typeof(SettingsViewModel).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            .Split('+')[0] ?? "0.1.0";

    partial void OnThemeChanged(ThemeChoice value)
    {
        _theme.ApplyTheme(value);
        Save();
    }

    partial void OnReduceMotionChanged(bool value)
    {
        _theme.ApplyReduceMotion(value);
        Save();
    }

    private void Save()
    {
        if (!_loading)
            _store.Save(new AppSettings { Theme = Theme, ReduceMotion = ReduceMotion });
    }
}
