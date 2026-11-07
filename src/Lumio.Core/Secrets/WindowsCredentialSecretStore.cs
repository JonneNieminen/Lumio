using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;

namespace Lumio.Core.Secrets;

/// <summary>Stores secrets as generic credentials in Windows Credential Manager.</summary>
[SupportedOSPlatform("windows")]
public sealed partial class WindowsCredentialSecretStore(string service = SecretStoreDefaults.Service) : ISecretStore
{
    private const uint CredTypeGeneric = 1;
    private const uint CredPersistLocalMachine = 2;
    private const int ErrorNotFound = 1168;

    public string DisplayName => "Windows Credential Manager";
    public bool IsPersistent => true;

    private string Target(string key) => $"{service}/{key}";

    public Task<string?> GetAsync(string key, CancellationToken ct = default) => Task.Run(() => Get(key), ct);
    public Task SetAsync(string key, string secret, CancellationToken ct = default) => Task.Run(() => Set(key, secret), ct);
    public Task DeleteAsync(string key, CancellationToken ct = default) => Task.Run(() => Delete(key), ct);

    private unsafe string? Get(string key)
    {
        if (!CredReadW(Target(key), CredTypeGeneric, 0, out var ptr))
        {
            var err = Marshal.GetLastPInvokeError();
            if (err == ErrorNotFound)
                return null;
            throw new SecretStoreException($"Credential Manager couldn't read the secret (error {err}).");
        }
        try
        {
            var cred = (Credential*)ptr;
            return Encoding.UTF8.GetString((byte*)cred->CredentialBlob, (int)cred->CredentialBlobSize);
        }
        finally
        {
            CredFree(ptr);
        }
    }

    private unsafe void Set(string key, string secret)
    {
        var blob = Encoding.UTF8.GetBytes(secret);
        var target = Marshal.StringToHGlobalUni(Target(key));
        var user = Marshal.StringToHGlobalUni(Environment.UserName);
        try
        {
            fixed (byte* b = blob)
            {
                var cred = new Credential
                {
                    Type = CredTypeGeneric,
                    TargetName = target,
                    CredentialBlobSize = (uint)blob.Length,
                    CredentialBlob = (IntPtr)b,
                    Persist = CredPersistLocalMachine,
                    UserName = user,
                };
                if (!CredWriteW(ref cred, 0))
                    throw new SecretStoreException(
                        $"Credential Manager couldn't save the secret (error {Marshal.GetLastPInvokeError()}).");
            }
        }
        finally
        {
            Marshal.FreeHGlobal(target);
            Marshal.FreeHGlobal(user);
        }
    }

    private void Delete(string key)
    {
        if (!CredDeleteW(Target(key), CredTypeGeneric, 0))
        {
            var err = Marshal.GetLastPInvokeError();
            if (err != ErrorNotFound)
                throw new SecretStoreException($"Credential Manager couldn't delete the secret (error {err}).");
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Credential
    {
        public uint Flags;
        public uint Type;
        public IntPtr TargetName;
        public IntPtr Comment;
        public long LastWritten;
        public uint CredentialBlobSize;
        public IntPtr CredentialBlob;
        public uint Persist;
        public uint AttributeCount;
        public IntPtr Attributes;
        public IntPtr TargetAlias;
        public IntPtr UserName;
    }

    [LibraryImport("advapi32.dll", EntryPoint = "CredReadW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool CredReadW(string target, uint type, uint flags, out IntPtr credential);

    [LibraryImport("advapi32.dll", EntryPoint = "CredWriteW", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool CredWriteW(ref Credential credential, uint flags);

    [LibraryImport("advapi32.dll", EntryPoint = "CredDeleteW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool CredDeleteW(string target, uint type, uint flags);

    [LibraryImport("advapi32.dll")]
    private static partial void CredFree(IntPtr buffer);
}
