namespace Quater.Desktop.Core.Auth.Storage;

public interface ITokenStore
{
    Task SaveAsync(TokenData data, CancellationToken ct = default);
    Task<TokenData?> GetAsync(CancellationToken ct = default);
    Task ClearAsync(CancellationToken ct = default);
}
