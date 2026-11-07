# Security policy

Lumio handles API keys and tokens, so security reports are taken seriously.

## Supported versions

Security fixes go into the latest release. Update to it before reporting.

## Reporting a vulnerability

Please **don't open a public issue**. Report it privately through
[GitHub's private vulnerability reporting](https://github.com/JonneNieminen/Lumio/security/advisories/new),
with the steps to reproduce, the affected platform and the impact you expect.

You'll get an acknowledgement within a few days. Once a fix is released, the advisory is published with credit, unless you'd rather stay anonymous.

## How Lumio protects secrets

- Secrets are stored only in the operating system's secure store: macOS Keychain, Windows Credential Manager, the Secret Service (libsecret) on Linux, or the Android Keystore.
- `connections.json` holds connection settings and check history, never secrets.
- A secret is sent only to the API it belongs to, and only when you test that connection.
- Lumio has no accounts, no telemetry and no backend of its own.
