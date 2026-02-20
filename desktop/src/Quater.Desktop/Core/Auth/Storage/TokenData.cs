namespace Quater.Desktop.Core.Auth.Storage;

public sealed record TokenData(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAtUtc
);
