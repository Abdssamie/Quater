using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Quater.Shared.Enums;
using Quater.Shared.Interfaces;

namespace Quater.Shared.Models;

/// <summary>
/// Represents a system user with role-based access.
/// Extends ASP.NET Core Identity IdentityUser.
/// </summary>
public class User : IdentityUser, IAuditable, IConcurrent
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
    /// UTC timestamp of last login
    /// </summary>
    public DateTime? LastLogin { get; set; }

    /// <summary>
    /// Whether account is active
    /// </summary>
    [Required]
    public bool IsActive { get; set; } = true;

    // IAuditable interface properties
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

    // IConcurrent interface properties
    [Timestamp]
    public byte[] RowVersion { get; set; } = null!;

    // Navigation properties
    public Lab Lab { get; init; } = null!;
    public ICollection<AuditLog> AuditLogs { get; init; } = new List<AuditLog>();
    public ICollection<AuditLogArchive> AuditLogArchives { get; init; } = new List<AuditLogArchive>();

}
