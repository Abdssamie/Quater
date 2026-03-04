using System.Text;
using Quater.Desktop.Core.Auth.Storage;
using Xunit;

namespace Quater.Desktop.Tests.Auth;

public sealed class SecureFileTokenStoreTests : IDisposable
{
    private readonly string _testTokenPath;

    public SecureFileTokenStoreTests()
    {
        _testTokenPath = Path.Combine(Path.GetTempPath(), "QuaterTests", Guid.NewGuid().ToString(), "tokens.dat");
    }

    public void Dispose()
    {
        var directory = Path.GetDirectoryName(_testTokenPath);
        if (directory != null && Directory.Exists(directory))
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public async Task SaveAsync_CreatesFile()
    {
        // Arrange
        var store = new SecureFileTokenStore(_testTokenPath);
        var data = new TokenData("access", "refresh", DateTime.UtcNow.AddHours(1));

        // Act
        await store.SaveAsync(data);

        // Assert
        Assert.True(File.Exists(_testTokenPath));
    }

    [Fact]
    public async Task GetAsync_WhenFileDoesNotExist_ReturnsNull()
    {
        // Arrange
        var store = new SecureFileTokenStore(_testTokenPath);

        // Act
        var result = await store.GetAsync();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task SaveAndGet_RoundTrip_Works()
    {
        // Arrange
        var store = new SecureFileTokenStore(_testTokenPath);
        var originalData = new TokenData("access-token", "refresh-token", new DateTime(2025, 12, 31, 23, 59, 59, DateTimeKind.Utc));

        // Act
        await store.SaveAsync(originalData);
        var retrievedData = await store.GetAsync();

        // Assert
        Assert.NotNull(retrievedData);
        Assert.Equal(originalData.AccessToken, retrievedData.AccessToken);
        Assert.Equal(originalData.RefreshToken, retrievedData.RefreshToken);
        Assert.Equal(originalData.ExpiresAtUtc, retrievedData.ExpiresAtUtc);
    }

    [Fact]
    public async Task ClearAsync_DeletesFile()
    {
        // Arrange
        var store = new SecureFileTokenStore(_testTokenPath);
        var data = new TokenData("access", "refresh", DateTime.UtcNow.AddHours(1));
        await store.SaveAsync(data);
        Assert.True(File.Exists(_testTokenPath));

        // Act
        await store.ClearAsync();

        // Assert
        Assert.False(File.Exists(_testTokenPath));
    }

    [Fact]
    public async Task ClearAsync_WhenFileDoesNotExist_DoesNotThrow()
    {
        // Arrange
        var store = new SecureFileTokenStore(_testTokenPath);
        Assert.False(File.Exists(_testTokenPath));

        // Act
        var exception = await Record.ExceptionAsync(() => store.ClearAsync());

        // Assert
        Assert.Null(exception);
    }
}
