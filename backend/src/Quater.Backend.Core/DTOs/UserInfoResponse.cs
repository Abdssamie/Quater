namespace Quater.Backend.Core.DTOs;

/// <summary>
/// OIDC standard UserInfo response
/// Contains only standard OIDC claims as per OpenID Connect specification
/// </summary>
public sealed class UserInfoResponse
{
    /// <summary>
    /// Subject - Unique identifier for the user (OIDC standard claim)
    /// </summary>
    public string Sub { get; set; } = string.Empty;
    
    /// <summary>
    /// User's display name (OIDC standard claim)
    /// </summary>
    public string? Name { get; set; }
    
    /// <summary>
    /// User's email address (OIDC standard claim)
    /// </summary>
    public string? Email { get; set; }
    
    /// <summary>
    /// Whether the email has been verified (OIDC standard claim)
    /// </summary>
    public bool EmailVerified { get; set; }
}
