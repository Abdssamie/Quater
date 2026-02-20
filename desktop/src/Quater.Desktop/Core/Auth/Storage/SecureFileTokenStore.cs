using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Quater.Desktop.Core.Auth.Storage;

public sealed class SecureFileTokenStore : ITokenStore
{
    private static readonly string TokenPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Quater",
        "tokens.dat");

    public async Task SaveAsync(TokenData data, CancellationToken ct = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(TokenPath)!);

        var json = JsonSerializer.Serialize(data);
        var payload = Encrypt(Encoding.UTF8.GetBytes(json));

        await File.WriteAllBytesAsync(TokenPath, payload, ct);

        if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
        {
            File.SetUnixFileMode(TokenPath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
    }

    public async Task<TokenData?> GetAsync(CancellationToken ct = default)
    {
        if (!File.Exists(TokenPath))
        {
            return null;
        }

        var payload = await File.ReadAllBytesAsync(TokenPath, ct);
        var bytes = Decrypt(payload);
        var json = Encoding.UTF8.GetString(bytes);

        return JsonSerializer.Deserialize<TokenData>(json);
    }

    public Task ClearAsync(CancellationToken ct = default)
    {
        if (File.Exists(TokenPath))
        {
            File.Delete(TokenPath);
        }

        return Task.CompletedTask;
    }

    private static byte[] Encrypt(byte[] plain)
    {
        using var aes = Aes.Create();
        var key = GetKey();
        aes.Key = key;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        var cipher = encryptor.TransformFinalBlock(plain, 0, plain.Length);

        var payload = new byte[aes.IV.Length + cipher.Length];
        Buffer.BlockCopy(aes.IV, 0, payload, 0, aes.IV.Length);
        Buffer.BlockCopy(cipher, 0, payload, aes.IV.Length, cipher.Length);
        return payload;
    }

    private static byte[] Decrypt(byte[] payload)
    {
        using var aes = Aes.Create();
        var key = GetKey();
        aes.Key = key;

        var iv = new byte[aes.BlockSize / 8];
        Buffer.BlockCopy(payload, 0, iv, 0, iv.Length);
        aes.IV = iv;

        var cipher = new byte[payload.Length - iv.Length];
        Buffer.BlockCopy(payload, iv.Length, cipher, 0, cipher.Length);

        using var decryptor = aes.CreateDecryptor();
        return decryptor.TransformFinalBlock(cipher, 0, cipher.Length);
    }

    private static byte[] GetKey()
    {
        var seed = Environment.MachineName + ":" + Environment.UserName + ":quater";
        using var sha256 = SHA256.Create();
        return sha256.ComputeHash(Encoding.UTF8.GetBytes(seed));
    }
}
