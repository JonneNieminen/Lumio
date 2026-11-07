using Android.Content;
using Android.Security.Keystore;
using Java.Security;
using Javax.Crypto;
using Javax.Crypto.Spec;
using Lumio.Core.Secrets;

namespace Lumio.Android;

/// <summary>
/// Encrypts secrets with an AES-256-GCM key that lives in the Android Keystore (hardware-backed
/// where available) and keeps only the ciphertext in private SharedPreferences.
/// </summary>
public sealed class AndroidKeystoreSecretStore(Context context) : ISecretStore
{
    private const string KeyStoreName = "AndroidKeyStore";
    private const string KeyAlias = "app.lumio.secrets";
    private const string Transformation = "AES/GCM/NoPadding";
    private const int TagBits = 128;

    private readonly ISharedPreferences _prefs =
        context.GetSharedPreferences("lumio_secrets", FileCreationMode.Private)!;

    public string DisplayName => "Android Keystore";
    public bool IsPersistent => true;

    public Task<string?> GetAsync(string key, CancellationToken ct = default)
    {
        var stored = _prefs.GetString(key, null);
        if (string.IsNullOrEmpty(stored))
            return Task.FromResult<string?>(null);
        try
        {
            var parts = stored.Split(':');
            var iv = Convert.FromBase64String(parts[0]);
            var cipherText = Convert.FromBase64String(parts[1]);
            var cipher = Cipher.GetInstance(Transformation)!;
            cipher.Init(CipherMode.DecryptMode, GetOrCreateKey(), new GCMParameterSpec(TagBits, iv));
            var plain = cipher.DoFinal(cipherText)!;
            return Task.FromResult<string?>(System.Text.Encoding.UTF8.GetString(plain));
        }
        catch (Exception ex) when (ex is GeneralSecurityException or FormatException or IndexOutOfRangeException)
        {
            throw new SecretStoreException("The Android Keystore couldn't decrypt the secret.", ex);
        }
    }

    public Task SetAsync(string key, string secret, CancellationToken ct = default)
    {
        try
        {
            var cipher = Cipher.GetInstance(Transformation)!;
            cipher.Init(CipherMode.EncryptMode, GetOrCreateKey());
            var cipherText = cipher.DoFinal(System.Text.Encoding.UTF8.GetBytes(secret))!;
            var value = $"{Convert.ToBase64String(cipher.GetIV()!)}:{Convert.ToBase64String(cipherText)}";
            _prefs.Edit()!.PutString(key, value)!.Apply();
            return Task.CompletedTask;
        }
        catch (GeneralSecurityException ex)
        {
            throw new SecretStoreException("The Android Keystore couldn't encrypt the secret.", ex);
        }
    }

    public Task DeleteAsync(string key, CancellationToken ct = default)
    {
        _prefs.Edit()!.Remove(key)!.Apply();
        return Task.CompletedTask;
    }

    private static IKey GetOrCreateKey()
    {
        var keyStore = KeyStore.GetInstance(KeyStoreName)!;
        keyStore.Load(null);
        if (keyStore.GetKey(KeyAlias, null) is { } existing)
            return existing;

        var generator = KeyGenerator.GetInstance(KeyProperties.KeyAlgorithmAes, KeyStoreName)!;
        generator.Init(new KeyGenParameterSpec.Builder(KeyAlias, KeyStorePurpose.Encrypt | KeyStorePurpose.Decrypt)
            .SetBlockModes(KeyProperties.BlockModeGcm)!
            .SetEncryptionPaddings(KeyProperties.EncryptionPaddingNone)!
            .SetKeySize(256)!
            .Build());
        return generator.GenerateKey()!;
    }
}
