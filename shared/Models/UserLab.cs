using System.ComponentModel.DataAnnotations;
using Quater.Shared.Enums;

namespace Quater.Shared.Models;

/// <summary>
/// Represents a user's membership and role within a specific lab.
/// </summary>
public class UserLab
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid LabId { get; set; }
    public Lab Lab { get; set; } = null!;

    [Required]
    public UserRole Role { get; set; }

    public DateTime AssignedAt { get; set; }
}
