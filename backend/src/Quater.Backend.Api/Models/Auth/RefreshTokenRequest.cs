using System.ComponentModel.DataAnnotations;

namespace Quater.Backend.Api.Models.Auth;

/// <summary>
/// Request model for refreshing access token
/// </summary>
public class RefreshTokenRequest
{
    /// <summary>
    /// Refresh token
    /// </summary>
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}
