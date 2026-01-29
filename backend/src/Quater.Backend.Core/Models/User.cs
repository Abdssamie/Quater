using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Quater.Backend.Core.Enums;

namespace Quater.Backend.Core.Models;

/// <summary>
/// Represents a system user with role-based access.
/// Extends ASP.NET Core Identity IdentityUser.
/// </summary>
public class User : IdentityUser
{
    /// <summary>
    /// User role for access control
    /// </summary>
    [Required]
    public UserRole Role { get; set; }

    /// <summary>
    /// Foreign key to Lab
    /// </summary>
    [Required]
    public Guid LabId { get; set; }

    /// <summary>
    /// UTC timestamp of account creation
    /// </summary>
    [Required]
    public DateTime CreatedDate { get; set; }

    /// <summary>
    /// UTC timestamp of last login
    /// </summary>
    public DateTime? LastLogin { get; set; }

    /// <summary>
    /// Whether account is active
    /// </summary>
    [Required]
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public Lab Lab { get; set; } = null!;
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
    public ICollection<AuditLogArchive> AuditLogArchives { get; set; } = new List<AuditLogArchive>();
    public ICollection<SyncLog> SyncLogs { get; set; } = new List<SyncLog>();
}
