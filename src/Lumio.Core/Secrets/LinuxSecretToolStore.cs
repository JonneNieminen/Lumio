using System.Diagnostics;
using System.Runtime.Versioning;

namespace Lumio.Core.Secrets;

/// <summary>
/// Stores secrets in the freedesktop Secret Service (GNOME Keyring, KWallet) through the
/// <c>secret-tool</c> command from libsecret. The secret is passed on stdin, never on the command line.
/// </summary>
[SupportedOSPlatform("linux")]
public sealed class LinuxSecretToolStore(string service = SecretStoreDefaults.Service) : ISecretStore
{
    public string DisplayName => "Secret Service (libsecret)";
    public bool IsPersistent => true;

    public static bool IsAvailable()
    {
        try
        {
            using var p = Process.Start(new ProcessStartInfo("secret-tool", "--version")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            });
            p?.WaitForExit(2000);
            return p is not null;
        }
        catch
        {
            return false;
        }
    }

    public async Task<string?> GetAsync(string key, CancellationToken ct = default)
    {
        var (code, output, _) = await RunAsync(["lookup", "service", service, "key", key], null, ct);
        return code == 0 ? output : null;
    }

    public async Task SetAsync(string key, string secret, CancellationToken ct = default)
    {
        var (code, _, error) = await RunAsync(
            ["store", $"--label=Lumio: {key}", "service", service, "key", key], secret, ct);
        if (code != 0)
            throw new SecretStoreException($"The Secret Service couldn't save the secret: {error.Trim()}");
    }

    public async Task DeleteAsync(string key, CancellationToken ct = default)
    {
        await RunAsync(["clear", "service", service, "key", key], null, ct);
    }

    private static async Task<(int Code, string Output, string Error)> RunAsync(
        string[] args, string? stdin, CancellationToken ct)
    {
        var psi = new ProcessStartInfo("secret-tool")
        {
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        foreach (var a in args)
            psi.ArgumentList.Add(a);

        using var process = Process.Start(psi)
            ?? throw new SecretStoreException("secret-tool could not be started. Install libsecret-tools.");
        if (stdin is not null)
            await process.StandardInput.WriteAsync(stdin.AsMemory(), ct);
        process.StandardInput.Close();

        var output = await process.StandardOutput.ReadToEndAsync(ct);
        var error = await process.StandardError.ReadToEndAsync(ct);
        await process.WaitForExitAsync(ct);
        return (process.ExitCode, output, error);
    }
}
