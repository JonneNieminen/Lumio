using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lumio.Core.Model;
using Lumio.Core.Services;
using Lumio.Services;

namespace Lumio.ViewModels;

/// <summary>
/// The guided "add a connection" flow: choose provider, authenticate, configure, test and save.
/// One step is lit at a time; the primary button always moves the user forward.
/// </summary>
public sealed partial class AddConnectionViewModel : PageViewModel
{
    public const int ChooseStep = 0;
    public const int AuthStep = 1;
    public const int ConfigureStep = 2;
    public const int TestStep = 3;

    private readonly ConnectionService _connections;
    private readonly INavigationService _navigation;
    private readonly IViewModelFactory _factory;
    private readonly IUrlLauncher _launcher;
    private bool _nameEdited;
    private bool _settingName;

    public AddConnectionViewModel(
        ConnectionService connections,
        INavigationService navigation,
        IViewModelFactory factory,
        IUrlLauncher launcher)
    {
        _connections = connections;
        _navigation = navigation;
        _factory = factory;
        _launcher = launcher;
    }

    public override string Title => "Add a connection";

    public IReadOnlyList<string> Steps { get; } = ["Choose provider", "Authenticate", "Configure", "Test and save"];

    public IReadOnlyList<Provider> Providers { get; } = ProviderCatalog.All;

    public string SecretStoreName => _connections.SecretStore.DisplayName;

    /// <summary>Where the secret will live, for the summary on the last step.</summary>
    public string SecretSummary => NeedsSecret ? SecretStoreName : "None needed";

    // ----- Step state -----

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsChooseStep), nameof(IsAuthStep), nameof(IsConfigureStep), nameof(IsTestStep),
        nameof(PrimaryText), nameof(BackText))]
    public partial int CurrentStep { get; private set; }

    public bool IsChooseStep => CurrentStep == ChooseStep;
    public bool IsAuthStep => CurrentStep == AuthStep;
    public bool IsConfigureStep => CurrentStep == ConfigureStep;
    public bool IsTestStep => CurrentStep == TestStep;

    public string PrimaryText => CurrentStep switch
    {
        ChooseStep => "Continue",
        AuthStep => "Continue",
        ConfigureStep => "Test connection",
        _ => "Save connection",
    };

    public string BackText => CurrentStep == ChooseStep ? "Cancel" : "Back";

    // ----- Step 1: provider -----

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCustom), nameof(ProviderName), nameof(SecretLabel), nameof(SecretPlaceholder),
        nameof(SecretHelp), nameof(HasDocs), nameof(DocsText), nameof(AuthHeading))]
    public partial Provider? SelectedProvider { get; set; }

    [ObservableProperty] public partial string? ProviderError { get; private set; }

    public bool IsCustom => SelectedProvider?.IsCustom == true;
    public string ProviderName => SelectedProvider?.Name ?? "";
    public string SecretLabel => SelectedProvider?.SecretLabel ?? "API key";
    public string SecretPlaceholder => SelectedProvider?.SecretPlaceholder ?? "";
    public string SecretHelp => SelectedProvider?.SecretHelp ?? "";
    public bool HasDocs => !string.IsNullOrEmpty(SelectedProvider?.DocsUrl);
    public string DocsText => $"Open {ProviderName} API docs";
    public string AuthHeading => IsCustom ? "How does this API sign requests?" : $"Connect to {ProviderName}";

    partial void OnSelectedProviderChanged(Provider? value)
    {
        if (value is null)
            return;
        ProviderError = null;
        AuthKind = value.AuthKind;
        BaseUrl = value.BaseUrl;
        HealthPath = value.HealthPath;
        if (!_nameEdited)
        {
            _settingName = true;
            Name = value.IsCustom ? "" : value.Name;
            _settingName = false;
        }
        TestResult = null;
    }

    // ----- Step 2: authentication -----

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NeedsSecret), nameof(NeedsUsername), nameof(NeedsHeaderName), nameof(IsPublic), nameof(SecretSummary))]
    public partial AuthKind AuthKind { get; set; }

    public bool NeedsSecret => AuthKind != AuthKind.None;
    public bool NeedsUsername => AuthKind == AuthKind.Basic;
    public bool NeedsHeaderName => AuthKind == AuthKind.ApiKeyHeader;
    public bool IsPublic => AuthKind == AuthKind.None;

    [ObservableProperty] public partial string Secret { get; set; } = "";
    [ObservableProperty] public partial bool IsSecretRevealed { get; set; }
    [ObservableProperty] public partial string Username { get; set; } = "";
    [ObservableProperty] public partial string ApiKeyHeader { get; set; } = "X-API-Key";
    [ObservableProperty] public partial string? SecretError { get; private set; }
    [ObservableProperty] public partial string? UsernameError { get; private set; }

    partial void OnSecretChanged(string value) => SecretError = null;
    partial void OnUsernameChanged(string value) => UsernameError = null;

    [RelayCommand]
    private void ToggleReveal() => IsSecretRevealed = !IsSecretRevealed;

    [RelayCommand]
    private Task OpenDocsAsync() =>
        SelectedProvider?.DocsUrl is { } url ? _launcher.OpenAsync(url) : Task.CompletedTask;

    // ----- Step 3: configure -----

    [ObservableProperty] public partial string Name { get; set; } = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EndpointPreview))]
    public partial string BaseUrl { get; set; } = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EndpointPreview))]
    public partial string HealthPath { get; set; } = "/";

    [ObservableProperty] public partial decimal? TimeoutSeconds { get; set; } = 10;
    [ObservableProperty] public partial string? NameError { get; private set; }
    [ObservableProperty] public partial string? BaseUrlError { get; private set; }

    public string EndpointPreview => string.IsNullOrWhiteSpace(BaseUrl) ? "—" : UrlUtil.Combine(BaseUrl, HealthPath);

    partial void OnNameChanged(string value)
    {
        NameError = null;
        if (!_settingName)
            _nameEdited = true;
    }

    partial void OnBaseUrlChanged(string value) => BaseUrlError = null;

    // ----- Step 4: test and save -----

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasResult), nameof(IsHealthy), nameof(IsDegraded), nameof(IsFailed),
        nameof(ResultTitle), nameof(ResultDetail))]
    public partial CheckResult? TestResult { get; private set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PrimaryCommand), nameof(TestAgainCommand))]
    public partial bool IsTesting { get; private set; }

    [ObservableProperty] public partial string? SaveError { get; private set; }

    public bool HasResult => TestResult is not null && !IsTesting;
    public bool IsHealthy => TestResult?.Outcome == CheckOutcome.Healthy;
    public bool IsDegraded => TestResult?.Outcome == CheckOutcome.Degraded;
    public bool IsFailed => TestResult?.Outcome == CheckOutcome.Failed;
    public string ResultTitle => TestResult?.Summary ?? "";

    public string ResultDetail => TestResult switch
    {
        null => "",
        { Advice: { } advice } => advice,
        { HttpStatus: { } code } => $"{UrlUtil.Combine(BaseUrl, HealthPath)} answered with HTTP {code}. Save the connection to keep watching it.",
        _ => "",
    };

    partial void OnIsTestingChanged(bool value) => OnPropertyChanged(nameof(HasResult));

    // ----- Commands -----

    private bool CanRunPrimary() => !IsTesting;

    [RelayCommand(CanExecute = nameof(CanRunPrimary))]
    private async Task PrimaryAsync()
    {
        switch (CurrentStep)
        {
            case ChooseStep when ValidateProvider():
                CurrentStep = AuthStep;
                break;
            case AuthStep when ValidateAuth():
                CurrentStep = ConfigureStep;
                break;
            case ConfigureStep when ValidateConfigure():
                CurrentStep = TestStep;
                await RunTestAsync();
                break;
            case TestStep:
                await SaveAsync();
                break;
        }
    }

    [RelayCommand]
    private Task BackAsync()
    {
        if (CurrentStep == ChooseStep)
            return _navigation.PopAsync();
        CurrentStep--;
        return Task.CompletedTask;
    }

    [RelayCommand(CanExecute = nameof(CanRunPrimary))]
    private Task TestAgainAsync() => RunTestAsync();

    internal ConnectionDraft BuildDraft() => new()
    {
        ProviderId = SelectedProvider?.Id ?? ProviderCatalog.CustomId,
        Name = string.IsNullOrWhiteSpace(Name) ? ProviderName : Name.Trim(),
        BaseUrl = BaseUrl.Trim(),
        HealthPath = string.IsNullOrWhiteSpace(HealthPath) ? "/" : HealthPath.Trim(),
        AuthKind = AuthKind,
        ApiKeyHeader = NeedsHeaderName ? ApiKeyHeader.Trim() : null,
        Username = NeedsUsername ? Username.Trim() : null,
        Secret = NeedsSecret ? Secret.Trim() : null,
        TimeoutSeconds = (int)Math.Clamp(TimeoutSeconds ?? 10, 1, 120),
        Headers = SelectedProvider?.Headers ?? new Dictionary<string, string>(),
    };

    private async Task RunTestAsync()
    {
        IsTesting = true;
        SaveError = null;
        try
        {
            TestResult = await _connections.TestDraftAsync(BuildDraft());
        }
        finally
        {
            IsTesting = false;
        }
    }

    private async Task SaveAsync()
    {
        try
        {
            var created = await _connections.CreateAsync(BuildDraft(), TestResult);
            var detail = _factory.Create<ConnectionDetailViewModel>();
            detail.ConnectionId = created.Id;
            await _navigation.ReplaceAsync(detail);
        }
        catch (Exception ex) when (ex is Lumio.Core.Secrets.SecretStoreException or IOException or UnauthorizedAccessException)
        {
            SaveError = $"Lumio couldn't save the connection. {ex.Message}";
        }
    }

    internal bool ValidateProvider()
    {
        ProviderError = SelectedProvider is null ? "Choose a provider to continue." : null;
        return ProviderError is null;
    }

    internal bool ValidateAuth()
    {
        SecretError = NeedsSecret && string.IsNullOrWhiteSpace(Secret)
            ? $"Add the {SecretLabel.ToLowerInvariant()} to continue."
            : null;
        UsernameError = NeedsUsername && string.IsNullOrWhiteSpace(Username) ? "Add the username to continue." : null;
        return SecretError is null && UsernameError is null;
    }

    internal bool ValidateConfigure()
    {
        NameError = string.IsNullOrWhiteSpace(Name) ? "Give the connection a name." : null;
        BaseUrlError = UrlUtil.IsHttpUrl(BaseUrl) ? null : "Use a full address that starts with https://";
        return NameError is null && BaseUrlError is null;
    }
}
