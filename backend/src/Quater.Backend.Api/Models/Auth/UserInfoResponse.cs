namespace Quater.Backend.Api.Models.Auth;

/// <summary>
/// Response model for user information
/// </summary>
public class UserInfoResponse
{
    /// <summary>
    /// User ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// User email address
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// User role
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// Lab ID the user belongs to
    /// </summary>
    public Guid LabId { get; set; }

    /// <summary>
    /// Whether the account is active
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// UTC timestamp of last login
    /// </summary>
    public DateTime? LastLogin { get; set; }
}
