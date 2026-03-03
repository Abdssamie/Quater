using System.Security.Cryptography;
using Quater.Desktop.Core.Auth.Storage;

namespace Quater.Desktop.Tests.Auth;

/// <summary>
/// Tests for SecureFileTokenStore using real file I/O on temp paths.
/// These tests verify AES-GCM encryption, key storage, and migration behavior.
/// </summary>
public sealed class SecureFileTokenStoreTests : IDisposable
{
    private readonly string _tempDir;

    public SecureFileTokenStoreTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"quater-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    private SecureFileTokenStore CreateStore() =>
        new(_tempDir);

    private static TokenData SampleToken() =>
        new("access-abc", "refresh-xyz", DateTime.UtcNow.AddHours(1));

    // -----------------------------------------------------------------------
    // Round-trip
    // -----------------------------------------------------------------------

    [Fact]
    public async Task SaveAndGet_RoundTrip_ReturnsOriginalData()
    {
        var store = CreateStore();
        var token = SampleToken();

        await store.SaveAsync(token);
        var retrieved = await store.GetAsync();

        Assert.NotNull(retrieved);
        Assert.Equal(token.AccessToken, retrieved!.AccessToken);
        Assert.Equal(token.RefreshToken, retrieved.RefreshToken);
        Assert.Equal(token.ExpiresAtUtc, retrieved.ExpiresAtUtc);
    }

    [Fact]
    public async Task GetAsync_WhenNoFile_ReturnsNull()
    {
        var store = CreateStore();

        var result = await store.GetAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task ClearAsync_DeletesTokenFile()
    {
        var store = CreateStore();
        await store.SaveAsync(SampleToken());

        await store.ClearAsync();
        var result = await store.GetAsync();

        Assert.Null(result);
    }

    // -----------------------------------------------------------------------
    // AES-GCM (authenticated encryption)
    // -----------------------------------------------------------------------

    [Fact]
    public async Task SaveAsync_FileContainsNonceAndTag_NotPlaintext()
    {
        var store = CreateStore();
        var token = SampleToken();

        await store.SaveAsync(token);

        var tokenPath = Path.Combine(_tempDir, "tokens.dat");
        Assert.True(File.Exists(tokenPath));

        var bytes = await File.ReadAllBytesAsync(tokenPath);
        // AES-GCM nonce = 12 bytes, tag = 16 bytes → minimum meaningful payload length
        Assert.True(bytes.Length > 12 + 16, "Payload too short to contain nonce + tag + ciphertext");

        // Must NOT contain the plaintext access token
        var raw = System.Text.Encoding.UTF8.GetString(bytes);
        Assert.DoesNotContain("access-abc", raw);
    }

    [Fact]
    public async Task SaveAsync_TwoSaves_ProduceDifferentCiphertexts()
    {
        // Verifies that a fresh nonce is generated per encrypt (IND-CPA)
        var store = CreateStore();
        var token = SampleToken();
        var tokenPath = Path.Combine(_tempDir, "tokens.dat");

        await store.SaveAsync(token);
        var first = await File.ReadAllBytesAsync(tokenPath);

        await store.SaveAsync(token);
        var second = await File.ReadAllBytesAsync(tokenPath);

        Assert.False(first.SequenceEqual(second), "Same plaintext must produce different ciphertexts (nonce reuse)");
    }

    [Fact]
    public async Task GetAsync_TamperedCiphertext_DeletesFileAndReturnsNull()
    {
        // AES-GCM authentication tag must detect tampering.
        // The store catches CryptographicException, deletes the corrupt file, and returns null.
        var store = CreateStore();
        await store.SaveAsync(SampleToken());

        var tokenPath = Path.Combine(_tempDir, "tokens.dat");
        var bytes = await File.ReadAllBytesAsync(tokenPath);

        // Flip a byte in the ciphertext area (beyond nonce+tag)
        bytes[^1] ^= 0xFF;
        await File.WriteAllBytesAsync(tokenPath, bytes);

        var result = await store.GetAsync();

        Assert.Null(result);
        Assert.False(File.Exists(tokenPath), "Corrupt/tampered token file must be deleted");
    }

    // -----------------------------------------------------------------------
    // Key storage: randomly generated, not derived from predictable inputs
    // -----------------------------------------------------------------------

    [Fact]
    public async Task KeyFile_IsCreatedAlongsideTokenFile()
    {
        var store = CreateStore();
        await store.SaveAsync(SampleToken());

        var keyPath = Path.Combine(_tempDir, "quater-keystore");
        Assert.True(File.Exists(keyPath), "Key file must exist after first save");
    }

    [Fact]
    public async Task KeyFile_ContainsRandomKey_NotDerivedFromMachineOrUser()
    {
        var store = CreateStore();
        await store.SaveAsync(SampleToken());

        var keyPath = Path.Combine(_tempDir, "quater-keystore");
        var keyBytes = await File.ReadAllBytesAsync(keyPath);

        // Key material must be at least 32 bytes (256 bits)
        // On Windows with DPAPI the file stores the protected blob; on Linux it stores the raw key (or wrapped)
        Assert.True(keyBytes.Length >= 32, "Key file must contain at least 32 bytes of key material");

        // Must NOT contain predictable seed strings
        var raw = System.Text.Encoding.UTF8.GetString(keyBytes);
        Assert.DoesNotContain(Environment.MachineName, raw);
        Assert.DoesNotContain(Environment.UserName, raw);
        Assert.DoesNotContain("quater", raw);
    }

    [Fact]
    public async Task TwoStores_SameDirectory_ShareKey_CanDecryptEachOther()
    {
        // Two store instances sharing the same dir must use the same persisted key
        var store1 = CreateStore();
        var store2 = CreateStore();
        var token = SampleToken();

        await store1.SaveAsync(token);
        var retrieved = await store2.GetAsync();

        Assert.NotNull(retrieved);
        Assert.Equal(token.AccessToken, retrieved!.AccessToken);
    }

    [Fact]
    public async Task TwoStores_DifferentDirectories_HaveDifferentKeys()
    {
        var dir2 = Path.Combine(Path.GetTempPath(), $"quater-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir2);
        try
        {
            var store1 = CreateStore();
            var store2 = new SecureFileTokenStore(dir2);
            var token = SampleToken();

            await store1.SaveAsync(token);

            var keyPath1 = Path.Combine(_tempDir, "quater-keystore");
            var keyPath2 = Path.Combine(dir2, "quater-keystore");

            // store2 has not been used yet; trigger key creation
            await store2.SaveAsync(token);

            var key1 = await File.ReadAllBytesAsync(keyPath1);
            var key2 = await File.ReadAllBytesAsync(keyPath2);

            Assert.False(key1.SequenceEqual(key2), "Different store directories must have different random keys");
        }
        finally
        {
            if (Directory.Exists(dir2))
                Directory.Delete(dir2, recursive: true);
        }
    }

    // -----------------------------------------------------------------------
    // Migration: old AES-CBC format is gracefully handled
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetAsync_WithOldCbcEncryptedFile_DeletesItAndReturnsNull()
    {
        // Write a token file encrypted with the old AES-CBC / SHA256(machine+user+quater) scheme
        var tokenPath = Path.Combine(_tempDir, "tokens.dat");
        var oldPayload = LegacyEncrypt(System.Text.Encoding.UTF8.GetBytes(
            """{"AccessToken":"old-access","RefreshToken":"old-refresh","ExpiresAtUtc":"2099-01-01T00:00:00Z"}"""));
        await File.WriteAllBytesAsync(tokenPath, oldPayload);

        var store = CreateStore();
        var result = await store.GetAsync();

        // Old format cannot be verified by AES-GCM — store must delete it and return null
        Assert.Null(result);
        Assert.False(File.Exists(tokenPath), "Old-format token file must be deleted during migration");
    }

    /// <summary>
    /// Replicates the original AES-CBC encryption so we can write legacy test fixtures.
    /// </summary>
    private static byte[] LegacyEncrypt(byte[] plain)
    {
        using var aes = System.Security.Cryptography.Aes.Create();
        var seed = Environment.MachineName + ":" + Environment.UserName + ":quater";
        using var sha = SHA256.Create();
        aes.Key = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(seed));
        aes.GenerateIV();

        using var enc = aes.CreateEncryptor();
        var cipher = enc.TransformFinalBlock(plain, 0, plain.Length);

        var payload = new byte[aes.IV.Length + cipher.Length];
        Buffer.BlockCopy(aes.IV, 0, payload, 0, aes.IV.Length);
        Buffer.BlockCopy(cipher, 0, payload, aes.IV.Length, cipher.Length);
        return payload;
    }
}
