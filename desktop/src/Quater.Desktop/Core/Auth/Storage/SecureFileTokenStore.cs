using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Quater.Desktop.Core.Auth.Storage;

/// <summary>
/// Stores authentication tokens on disk using AES-GCM authenticated encryption.
///
/// Key management:
///   - Windows: a random 32-byte key is generated once, then protected with DPAPI
///              (DataProtectionScope.CurrentUser) and stored in a sidecar key file.
///   - Linux/macOS: a random 32-byte key is stored in a sidecar key file with
///                  chmod 600 permissions (readable only by the owning OS user).
///
/// Migration: if an existing token file was encrypted with the legacy AES-CBC scheme
/// it cannot be authenticated by AES-GCM. The old file is deleted automatically so
/// the user is simply asked to log in again.
/// </summary>
public sealed class SecureFileTokenStore : ITokenStore
{
    // AES-GCM constants (standard values; not using AesGcm.*ByteSizes which are not const)
    private const int NonceSize = 12; // AES-GCM standard nonce (96-bit)
    private const int TagSize   = 16; // AES-GCM full authentication tag (128-bit)
    private const int KeySize   = 32; // 256-bit key

    private readonly string _tokenPath;
    private readonly string _keyPath;

    // Default constructor: uses the standard AppData location (production usage).
    public SecureFileTokenStore()
        : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Quater"))
    {
    }

    // Overload accepting an explicit directory – used by tests and for testability.
    public SecureFileTokenStore(string directory)
    {
        _tokenPath = Path.Combine(directory, "tokens.dat");
        _keyPath   = Path.Combine(directory, "quater-keystore");
    }

    // -----------------------------------------------------------------------
    // ITokenStore
    // -----------------------------------------------------------------------

    public async Task SaveAsync(TokenData data, CancellationToken ct = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_tokenPath)!);

        byte[] payload;
        try
        {
            var key = LoadOrCreateKey();
            var json = JsonSerializer.Serialize(data);
            payload = Encrypt(Encoding.UTF8.GetBytes(json), key);
        }
        catch (CryptographicException)
        {
            // Corrupt key file (e.g., DPAPI failure): wipe both files so a fresh
            // key is generated on the next attempt, rather than looping forever.
            DeleteStaleState();
            throw;
        }

        await File.WriteAllBytesAsync(_tokenPath, payload, ct);
        SetRestrictedPermissions(_tokenPath);
    }

    public async Task<TokenData?> GetAsync(CancellationToken ct = default)
    {
        if (!File.Exists(_tokenPath))
            return null;

        var payload = await File.ReadAllBytesAsync(_tokenPath, ct);

        byte[] plaintext;
        try
        {
            var key = LoadOrCreateKey();
            plaintext = Decrypt(payload, key);
        }
        catch (CryptographicException)
        {
            // Legacy AES-CBC file, tampered ciphertext, or corrupt key file:
            // wipe both files and force re-login. A fresh key will be generated
            // on the next SaveAsync call.
            DeleteStaleState();
            return null;
        }

        var json = Encoding.UTF8.GetString(plaintext);
        return JsonSerializer.Deserialize<TokenData>(json);
    }

    public Task ClearAsync(CancellationToken ct = default)
    {
        if (File.Exists(_tokenPath))
            File.Delete(_tokenPath);

        if (File.Exists(_keyPath))
            File.Delete(_keyPath);

        return Task.CompletedTask;
    }

    // -----------------------------------------------------------------------
    // Encryption / Decryption  (AES-GCM)
    // -----------------------------------------------------------------------

    // Wire format: [ nonce (12) | tag (16) | ciphertext (n) ]
    private static byte[] Encrypt(byte[] plaintext, byte[] key)
    {
        var nonce      = new byte[NonceSize];
        var tag        = new byte[TagSize];
        var ciphertext = new byte[plaintext.Length];

        RandomNumberGenerator.Fill(nonce);

        using var gcm = new AesGcm(key, TagSize);
        gcm.Encrypt(nonce, plaintext, ciphertext, tag);

        var payload = new byte[NonceSize + TagSize + ciphertext.Length];
        Buffer.BlockCopy(nonce,      0, payload, 0,                   NonceSize);
        Buffer.BlockCopy(tag,        0, payload, NonceSize,            TagSize);
        Buffer.BlockCopy(ciphertext, 0, payload, NonceSize + TagSize,  ciphertext.Length);
        return payload;
    }

    private static byte[] Decrypt(byte[] payload, byte[] key)
    {
        if (payload.Length < NonceSize + TagSize)
            throw new CryptographicException("Payload too short to be a valid AES-GCM ciphertext.");

        var nonce      = payload[..NonceSize];
        var tag        = payload[NonceSize..(NonceSize + TagSize)];
        var ciphertext = payload[(NonceSize + TagSize)..];
        var plaintext  = new byte[ciphertext.Length];

        using var gcm = new AesGcm(key, TagSize);
        gcm.Decrypt(nonce, ciphertext, tag, plaintext); // throws CryptographicException on auth failure
        return plaintext;
    }

    // -----------------------------------------------------------------------
    // Key management
    // -----------------------------------------------------------------------

    private byte[] LoadOrCreateKey()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_keyPath)!);

        if (File.Exists(_keyPath))
            return ReadKey();

        return CreateAndPersistKey();
    }

    private byte[] CreateAndPersistKey()
    {
        var rawKey = RandomNumberGenerator.GetBytes(KeySize);
        WriteKey(rawKey);
        return rawKey;
    }

    private byte[] ReadKey()
    {
        var stored = File.ReadAllBytes(_keyPath);

        if (OperatingSystem.IsWindows())
        {
#pragma warning disable CA1416 // Validated by OperatingSystem.IsWindows()
            return System.Security.Cryptography.ProtectedData.Unprotect(
                stored,
                optionalEntropy: null,
                scope: System.Security.Cryptography.DataProtectionScope.CurrentUser);
#pragma warning restore CA1416
        }

        // Linux / macOS: raw key bytes stored with 600 permissions
        if (stored.Length != KeySize)
            throw new CryptographicException($"Key file has unexpected length {stored.Length}; expected {KeySize}.");

        return stored;
    }

    private void WriteKey(byte[] rawKey)
    {
        byte[] toStore;

        if (OperatingSystem.IsWindows())
        {
#pragma warning disable CA1416
            toStore = System.Security.Cryptography.ProtectedData.Protect(
                rawKey,
                optionalEntropy: null,
                scope: System.Security.Cryptography.DataProtectionScope.CurrentUser);
#pragma warning restore CA1416
        }
        else
        {
            toStore = rawKey;
        }

        File.WriteAllBytes(_keyPath, toStore);
        SetRestrictedPermissions(_keyPath);
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static void SetRestrictedPermissions(string path)
    {
        if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
        {
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
    }

    /// <summary>
    /// Deletes both the token file and the key file so that the next operation
    /// starts completely fresh. Called whenever a <see cref="CryptographicException"/>
    /// indicates that the on-disk state is unrecoverable (corrupt key, tampered
    /// ciphertext, legacy AES-CBC format, etc.).
    /// </summary>
    private void DeleteStaleState()
    {
        if (File.Exists(_tokenPath)) File.Delete(_tokenPath);
        if (File.Exists(_keyPath))   File.Delete(_keyPath);
    }
}
