# Lumio

Connect any API, one lit step at a time.

Lumio is an Avalonia 12 / .NET 10 app for macOS, Windows, Linux and Android. A guided wizard
connects an API, the dashboard shows every connection's health, and secrets stay in the
operating system's secure store.

## Run

```bash
dotnet run --project src/Lumio.Desktop
```

```bash
dotnet test tests/Lumio.Tests
```

Android needs the workload and a JDK 17+ (plus the Android SDK, already at `~/Library/Android/sdk`):

```bash
sudo dotnet workload install android
```

```bash
dotnet build src/Lumio.Android -t:Run
```

## Layout

| Project | What it holds |
| --- | --- |
| `src/Lumio.Core` | Domain model, provider catalog, JSON storage, secret stores, HTTP health checks. No UI references. |
| `src/Lumio.Theme` | The Lumio design system for Avalonia: generated tokens, Fluent palette mapping, control styles, `Stepper`, `StatusBadge`, `LumioMark`. |
| `src/Lumio` | App shell, pages and view models (CommunityToolkit.Mvvm, compiled bindings, MS DI). |
| `src/Lumio.Desktop` | Desktop head. Picks Keychain, Credential Manager or Secret Service. DevTools in Debug. |
| `src/Lumio.Android` | Android head with an Android Keystore secret store. |
| `tests/Lumio.Tests` | xUnit v3 tests for Core, view models and headless UI (theme tokens, stepper). |

Navigation uses Avalonia's `DrawerPage` + `NavigationPage`, driven view-model-first by
`NavigationService` (explicit view mapping, trimming safe). The drawer is an inline sidebar on
wide windows and an overlay on phones; pages adapt with container queries.

## Theme

`../lumio-theme/tokens.json` is the source of truth. After changing it:

```bash
python3 ../lumio-theme/generate_theme.py && python3 tools/generate_avalonia_tokens.py
```

This rewrites `src/Lumio.Theme/Tokens/Tokens.g.axaml`: every token as `Lm<Name>Color` /
`Lm<Name>Brush` per theme (Nightshift = Dark, Daybreak = Light), shadows and glow as
`BoxShadows`, and Fluent resource keys mapped to Lumio tokens.

## CI and releases

| Workflow | When | What |
| --- | --- | --- |
| `.github/dependabot.yml` | Weekly, Monday | NuGet packages and .NET tools (Avalonia and Velopack/vpk grouped so they move together), the .NET SDK in `global.json` (minor and patch only) and GitHub Actions. |
| `Build` | Push to main, PRs (including Dependabot's), manual | Tests, then self-contained builds for win-x64/arm64, osx-arm64/x64, linux-x64/arm64 and an Android APK, plus the macOS DMGs, as run artifacts. The `Build result` check is green only when every job passed. Make it a required check for `main` so Dependabot PRs merge only after the full matrix builds. |
| `Release` | Manual (Actions → Release → Run workflow) | Picks the version (latest `vX.Y.Z` tag bumped by patch/minor/major, or an explicit version), tests, packs every desktop channel with Velopack (deltas from the previous release), builds the APK and macOS DMGs, and publishes a GitHub release with install notes, using the `GHTOKENWORKFLOW` secret. |

The GitHub release is the update feed: each desktop channel (`win-x64`, `osx-arm64`, …) has its own
`releases.<channel>.json`. In-app updating (an `IAppUpdateService` over Velopack's `GithubSource`)
is the next step. `VelopackApp.Build().Run()` is already the first line of `Main`. Builds are
unsigned for now; `release.yml` marks where Windows, macOS and Android signing go.

macOS DMGs come from `build/macos/make-dmg.sh`: Lumio.app, an Applications shortcut and
`READ ME FIRST.txt` with the command that lets an unnotarized build open:

```bash
xattr -dr com.apple.quarantine /Applications/Lumio.app
```

The Velopack package id is `Lumio.Desktop`. Never change it, or installed apps stop finding
updates. The `Velopack` NuGet package and the `vpk` tool (`.config/dotnet-tools.json`) must stay on
the same version.

## Data

- Connection metadata and check history: `connections.json` in the app data folder
  (`~/Library/Application Support/Lumio` on macOS).
- Secrets: OS secure store only, service `app.lumio`, account `connection.<id>`.
