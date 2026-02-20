using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Quater.Shared.Interfaces;

namespace Quater.Shared.Models;

/// <summary>
/// Represents a system user with role-based access.
/// Extends ASP.NET Core Identity IdentityUser.
/// </summary>
public class User : IdentityUser<Guid>, IConcurrent
{
    /// <summary>
    /// UTC timestamp of last login
    /// </summary>
    public DateTime? LastLogin { get; set; }

    /// <summary>
    /// Whether account is active
    /// </summary>
    [Required]
    public bool IsActive { get; set; } = true;

    // IConcurrent interface properties
    [Timestamp]
    public byte[] RowVersion { get; set; } = null!;

    // Navigation properties
    public ICollection<UserLab> UserLabs { get; init; } = [];
    public ICollection<AuditLog> AuditLogs { get; init; } = [];
    public ICollection<AuditLogArchive> AuditLogArchives { get; init; } = [];
    public ICollection<UserInvitation> SentInvitations { get; init; } = [];
    public UserInvitation? ReceivedInvitation { get; set; }
}
