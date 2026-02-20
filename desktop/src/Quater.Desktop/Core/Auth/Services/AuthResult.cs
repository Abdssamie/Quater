namespace Quater.Desktop.Core.Auth.Services;

public sealed record AuthResult(
    bool IsError,
    string? Error,
    string? AccessToken,
    string? RefreshToken,
    DateTime? ExpiresAtUtc
);
