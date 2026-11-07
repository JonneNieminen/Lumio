namespace Lumio.Core.Model;

/// <summary>The built-in list of providers offered in the connection wizard.</summary>
public static class ProviderCatalog
{
    public const string CustomId = "custom";

    public static IReadOnlyList<Provider> All { get; } =
    [
        new()
        {
            Id = "github",
            Name = "GitHub",
            Monogram = "Gh",
            Description = "Repositories, issues and workflow runs",
            BaseUrl = "https://api.github.com",
            HealthPath = "/user",
            AuthKind = AuthKind.Bearer,
            SecretLabel = "Personal access token",
            SecretPlaceholder = "github_pat_…",
            SecretHelp = "Create a fine-grained token in GitHub settings. Read-only access is enough.",
            DocsUrl = "https://docs.github.com/rest",
            Headers = new Dictionary<string, string>
            {
                ["Accept"] = "application/vnd.github+json",
                ["X-GitHub-Api-Version"] = "2022-11-28",
            },
        },
        new()
        {
            Id = "stripe",
            Name = "Stripe",
            Monogram = "St",
            Description = "Payments, customers and billing",
            BaseUrl = "https://api.stripe.com",
            HealthPath = "/v1/balance",
            AuthKind = AuthKind.Bearer,
            SecretLabel = "Secret key",
            SecretPlaceholder = "sk_test_… or rk_live_…",
            SecretHelp = "Use a restricted key with read access to Balance. Test-mode keys work too.",
            DocsUrl = "https://docs.stripe.com/api",
        },
        new()
        {
            Id = "huggingface",
            Name = "Hugging Face",
            Monogram = "Hf",
            Description = "Models, datasets and inference",
            BaseUrl = "https://huggingface.co",
            HealthPath = "/api/whoami-v2",
            AuthKind = AuthKind.Bearer,
            SecretLabel = "Access token",
            SecretPlaceholder = "hf_…",
            SecretHelp = "Create a read token in your Hugging Face account settings.",
            DocsUrl = "https://huggingface.co/docs/hub/api",
        },
        new()
        {
            Id = "jsonplaceholder",
            Name = "JSONPlaceholder",
            Monogram = "Jp",
            Description = "Free sample REST API, good for a first try",
            BaseUrl = "https://jsonplaceholder.typicode.com",
            HealthPath = "/todos/1",
            AuthKind = AuthKind.None,
            DocsUrl = "https://jsonplaceholder.typicode.com/guide/",
        },
        new()
        {
            Id = CustomId,
            Name = "Custom REST API",
            Monogram = "{}",
            Description = "Any HTTP API with a health or status endpoint",
            BaseUrl = "",
            HealthPath = "/health",
            AuthKind = AuthKind.Bearer,
            IsCustom = true,
            SecretLabel = "Secret",
            SecretPlaceholder = "Token, key or password",
        },
    ];

    public static Provider Get(string id) =>
        All.FirstOrDefault(p => p.Id == id) ?? All.First(p => p.Id == CustomId);
}
