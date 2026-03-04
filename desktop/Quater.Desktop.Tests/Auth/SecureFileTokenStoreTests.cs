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

    [Fact]
    public async Task ClearAsync_DeletesKeyFile()
    {
        var store = CreateStore();
        await store.SaveAsync(SampleToken());

        var keyPath = Path.Combine(_tempDir, "quater-keystore");
        Assert.True(File.Exists(keyPath));

        await store.ClearAsync();

        Assert.False(File.Exists(keyPath));
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

        // Key file must hold at least 32 bytes of material (DPAPI blob is larger on Windows)
        Assert.True(keyBytes.Length >= 32, "Key file must contain at least 32 bytes of key material");

        // Key must not be all-zeros
        Assert.False(keyBytes.All(b => b == 0), "Key file must not be all-zeros");

        // Key must NOT equal the legacy SHA256(MachineName+UserName+"quater") derivation
        var legacySeed = System.Text.Encoding.UTF8.GetBytes(
            Environment.MachineName + ":" + Environment.UserName + ":quater");
        var legacyKey = SHA256.HashData(legacySeed);

        // On Linux/macOS the key file is exactly 32 bytes; compare directly.
        // On Windows the file is a DPAPI blob (larger), so the lengths already differ.
        if (keyBytes.Length == legacyKey.Length)
        {
            Assert.False(keyBytes.SequenceEqual(legacyKey),
                "Key must not be derived from predictable MachineName+UserName seed");
        }
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
    // Stale-state cleanup (corrupt key file)
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetAsync_WithCorruptKeyFile_DeletesBothFilesAndReturnsNull()
    {
        // Arrange: create a valid token file, then overwrite the key file with garbage
        var store = CreateStore();
        await store.SaveAsync(SampleToken());

        var tokenPath = Path.Combine(_tempDir, "tokens.dat");
        var keyPath   = Path.Combine(_tempDir, "quater-keystore");

        // Corrupt the key file (wrong length triggers CryptographicException on Linux/macOS;
        // DPAPI failure on Windows also throws CryptographicException)
        await File.WriteAllBytesAsync(keyPath, [0x00, 0x01, 0x02]); // 3 bytes – invalid

        // Act
        var result = await store.GetAsync();

        // Assert: both files deleted, store returns null (will re-generate key on next save)
        Assert.Null(result);
        Assert.False(File.Exists(tokenPath), "Token file must be deleted when key file is corrupt");
        Assert.False(File.Exists(keyPath),   "Key file must be deleted when it is corrupt");
    }

    [Fact]
    public async Task SaveAsync_AfterCorruptKeyFileCleanup_RecreatesKeyAndSucceeds()
    {
        // Arrange: set up a corrupt key file with no token file
        var keyPath   = Path.Combine(_tempDir, "quater-keystore");
        var tokenPath = Path.Combine(_tempDir, "tokens.dat");
        await File.WriteAllBytesAsync(keyPath, [0xDE, 0xAD]); // corrupt

        var store = CreateStore();

        // Act: SaveAsync must detect the corrupt key, wipe state, then rethrow.
        // On the *second* call (after cleanup) it should succeed end-to-end.
        await Assert.ThrowsAnyAsync<CryptographicException>(() => store.SaveAsync(SampleToken()));

        // Both files were deleted by the first (failing) SaveAsync
        Assert.False(File.Exists(keyPath),   "Corrupt key file must be deleted by SaveAsync");
        Assert.False(File.Exists(tokenPath), "Partial token file must not exist after failed save");

        // Second save regenerates everything cleanly
        await store.SaveAsync(SampleToken());
        var retrieved = await store.GetAsync();
        Assert.NotNull(retrieved);
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
