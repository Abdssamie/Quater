using System.ComponentModel.DataAnnotations;
using Quater.Shared.Enums;

namespace Quater.Backend.Core.DTOs;

/// <summary>
/// Data Transfer Object for User entity
/// </summary>
public class UserDto
{
    public Guid Id { get; set; }
    public string? UserName { get; set; }
    public string? Email { get; set; }
    public UserRole Role { get; set; }
    public Guid LabId { get; set; }
    public string? LabName { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? LastLogin { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
}

/// <summary>
/// DTO for creating a new user
/// </summary>
public class CreateUserDto
{
    [Required(ErrorMessage = "Username is required")]
    [MaxLength(256, ErrorMessage = "Username cannot exceed 256 characters")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email address")]
    [MaxLength(256, ErrorMessage = "Email cannot exceed 256 characters")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Role is required")]
    public UserRole Role { get; set; }

    [Required(ErrorMessage = "Lab ID is required")]
    public Guid LabId { get; set; }
}

/// <summary>
/// DTO for updating an existing user
/// </summary>
public class UpdateUserDto
{
    [MaxLength(256, ErrorMessage = "Username cannot exceed 256 characters")]
    public string? UserName { get; set; }

    [EmailAddress(ErrorMessage = "Invalid email address")]
    [MaxLength(256, ErrorMessage = "Email cannot exceed 256 characters")]
    public string? Email { get; set; }

    public UserRole? Role { get; set; }

    public Guid? LabId { get; set; }

    public bool? IsActive { get; set; }
}

/// <summary>
/// DTO for changing user password
/// </summary>
public class ChangePasswordDto
{
    [Required(ErrorMessage = "Current password is required")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "New password is required")]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password confirmation is required")]
    [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
