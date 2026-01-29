using System.ComponentModel.DataAnnotations;

namespace Quater.Backend.Api.Models.Auth;

/// <summary>
/// Request model for user registration
/// </summary>
public class RegisterRequest
{
    /// <summary>
    /// User email address
    /// </summary>
    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// User password (min 8 chars, requires uppercase, lowercase, digit, special char)
    /// </summary>
    [Required]
    [MinLength(8)]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// User role: Admin, Technician, or Viewer
    /// </summary>
    [Required]
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// Lab ID the user belongs to
    /// </summary>
    [Required]
    public Guid LabId { get; set; }
}
