# Contributing to Lumio

Thanks for helping! Lumio is a small project, so the process is light.

## Setup

You need the [.NET 10 SDK](https://dotnet.microsoft.com/download) (the version is pinned in `global.json`).

```bash
dotnet run --project src/Lumio.Desktop
```

Android also needs `sudo dotnet workload install android`, a JDK 17+ and the Android SDK.

## Before you open a pull request

```bash
dotnet build src/Lumio.Desktop -warnaserror
dotnet test --project tests/Lumio.Tests
```

CI runs the tests and builds every platform (Windows, macOS and Linux on x64 and ARM, plus Android). The `Build result` check must be green.

## Guidelines

- **Core stays UI-free.** `Lumio.Core` has no Avalonia references. View models depend on interfaces (`ISecretStore`, `IConnectionTester`, …), never on platform APIs.
- **Secrets never touch Lumio's files.** Keys go through `ISecretStore` only, and are never logged, serialized or shown unmasked by default.
- **Design tokens only.** Colors, spacing and radii come from `lumio-theme/tokens.json` via `Tokens.g.axaml`. Use `Lm…Brush` resources and the theme's control classes, never raw hex values in views. Regenerate with `python3 tools/generate_avalonia_tokens.py`.
- **One light at a time.** Exactly one accent element (primary button, current step) per view.
- **Compiled bindings.** Every view sets `x:DataType`. Navigation goes through `NavigationService`, with an explicit view mapping.
- **Voice.** Plain, warm and direct. Verb-first buttons ("Test connection"), errors that say what happened and what to do next, no exclamation marks.
- **Tests with changes.** Core logic gets unit tests. View models get tests with the fakes in `tests/Lumio.Tests/Fakes.cs`. Visual building blocks get headless UI tests.
- **Packages** use Central Package Management (`Directory.Packages.props`). Keep `Velopack` and the `vpk` tool on the same version.

## Reporting bugs

Use the bug report template. Never paste real API keys or tokens into an issue. For security problems, follow [SECURITY.md](SECURITY.md) instead of opening a public issue.
