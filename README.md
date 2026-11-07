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

## Data

- Connection metadata and check history: `connections.json` in the app data folder
  (`~/Library/Application Support/Lumio` on macOS).
- Secrets: OS secure store only, service `app.lumio`, account `connection.<id>`.
