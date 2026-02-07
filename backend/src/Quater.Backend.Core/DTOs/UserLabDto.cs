using System.ComponentModel.DataAnnotations;
using Quater.Shared.Enums;

namespace Quater.Backend.Core.DTOs;

/// <summary>
/// Represents a user's membership in a specific lab with their role.
/// </summary>
public class UserLabDto
{
    public Guid LabId { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string LabName { get; set; } = string.Empty;
    
    [Required]
    public UserRole Role { get; set; }
    
    public DateTime AssignedAt { get; set; }
}
