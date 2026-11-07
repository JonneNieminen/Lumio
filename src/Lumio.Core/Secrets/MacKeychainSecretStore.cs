using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;

namespace Lumio.Core.Secrets;

/// <summary>
/// Stores secrets as generic passwords in the user's login keychain (Security.framework).
/// </summary>
[SupportedOSPlatform("macos")]
public sealed partial class MacKeychainSecretStore(string service = SecretStoreDefaults.Service) : ISecretStore
{
    private const int ErrSecSuccess = 0;
    private const int ErrSecItemNotFound = -25300;
    private const string Security = "/System/Library/Frameworks/Security.framework/Security";
    private const string CoreFoundation = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";

    private readonly byte[] _service = Encoding.UTF8.GetBytes(service);

    public string DisplayName => "macOS Keychain";
    public bool IsPersistent => true;

    public Task<string?> GetAsync(string key, CancellationToken ct = default) => Task.Run(() => Get(key), ct);

    public Task SetAsync(string key, string secret, CancellationToken ct = default) => Task.Run(() => Set(key, secret), ct);

    public Task DeleteAsync(string key, CancellationToken ct = default) => Task.Run(() => Delete(key), ct);

    private unsafe string? Get(string key)
    {
        var account = Encoding.UTF8.GetBytes(key);
        uint length = 0;
        void* data = null;
        var status = SecKeychainFindGenericPassword(IntPtr.Zero, (uint)_service.Length, _service,
            (uint)account.Length, account, &length, &data, null);
        if (status == ErrSecItemNotFound)
            return null;
        Check(status, "read");
        try
        {
            return Encoding.UTF8.GetString((byte*)data, (int)length);
        }
        finally
        {
            SecKeychainItemFreeContent(IntPtr.Zero, data);
        }
    }

    private unsafe void Set(string key, string secret)
    {
        var account = Encoding.UTF8.GetBytes(key);
        var password = Encoding.UTF8.GetBytes(secret);
        IntPtr item = IntPtr.Zero;
        var status = SecKeychainFindGenericPassword(IntPtr.Zero, (uint)_service.Length, _service,
            (uint)account.Length, account, null, null, &item);
        try
        {
            if (status == ErrSecSuccess)
            {
                fixed (byte* p = password)
                    Check(SecKeychainItemModifyAttributesAndData(item, IntPtr.Zero, (uint)password.Length, p), "update");
                return;
            }
            if (status != ErrSecItemNotFound)
                Check(status, "read");

            fixed (byte* p = password)
                Check(SecKeychainAddGenericPassword(IntPtr.Zero, (uint)_service.Length, _service,
                    (uint)account.Length, account, (uint)password.Length, p, null), "save");
        }
        finally
        {
            if (item != IntPtr.Zero)
                CFRelease(item);
        }
    }

    private unsafe void Delete(string key)
    {
        var account = Encoding.UTF8.GetBytes(key);
        IntPtr item = IntPtr.Zero;
        var status = SecKeychainFindGenericPassword(IntPtr.Zero, (uint)_service.Length, _service,
            (uint)account.Length, account, null, null, &item);
        if (status == ErrSecItemNotFound)
            return;
        Check(status, "read");
        try
        {
            Check(SecKeychainItemDelete(item), "delete");
        }
        finally
        {
            CFRelease(item);
        }
    }

    private static void Check(int status, string action)
    {
        if (status != ErrSecSuccess)
            throw new SecretStoreException($"The keychain couldn't {action} the secret (OSStatus {status}).");
    }

    // The SecKeychain* generic-password calls are deprecated in favour of SecItem*, but remain
    // supported and avoid building CFDictionary queries by hand.
    [LibraryImport(Security)]
    private static unsafe partial int SecKeychainFindGenericPassword(IntPtr keychainOrArray,
        uint serviceNameLength, byte[] serviceName, uint accountNameLength, byte[] accountName,
        uint* passwordLength, void** passwordData, IntPtr* itemRef);

    [LibraryImport(Security)]
    private static unsafe partial int SecKeychainAddGenericPassword(IntPtr keychain,
        uint serviceNameLength, byte[] serviceName, uint accountNameLength, byte[] accountName,
        uint passwordLength, void* passwordData, IntPtr* itemRef);

    [LibraryImport(Security)]
    private static unsafe partial int SecKeychainItemModifyAttributesAndData(IntPtr itemRef,
        IntPtr attrList, uint length, void* data);

    [LibraryImport(Security)]
    private static partial int SecKeychainItemDelete(IntPtr itemRef);

    [LibraryImport(Security)]
    private static unsafe partial int SecKeychainItemFreeContent(IntPtr attrList, void* data);

    [LibraryImport(CoreFoundation)]
    private static partial void CFRelease(IntPtr cf);
}
