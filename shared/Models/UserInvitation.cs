using System.ComponentModel.DataAnnotations;
using Quater.Shared.Enums;
using Quater.Shared.Interfaces;

namespace Quater.Shared.Models;

/// <summary>
/// Represents a user invitation with pre-assigned lab roles.
/// Invitations are created by admins and accepted by users to activate their accounts.
/// </summary>
public class UserInvitation : IConcurrent, IAuditable
{
    /// <summary>
    /// Unique identifier for the invitation.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Foreign key to the invited user (created immediately upon invitation).
    /// </summary>
    [Required]
    public Guid UserId { get; set; }

    /// <summary>
    /// Email address of the invited user.
    /// </summary>
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Hashed invitation token (SHA256) for secure storage.
    /// Plain token is sent in email, hashed token stored in database.
    /// </summary>
    [Required]
    public string TokenHash { get; set; } = string.Empty;

    /// <summary>
    /// UTC timestamp when the invitation expires (7 days from creation).
    /// </summary>
    [Required]
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Current status of the invitation.
    /// </summary>
    [Required]
    public InvitationStatus Status { get; set; }

    /// <summary>
    /// Foreign key to the admin user who sent the invitation.
    /// </summary>
    [Required]
    public Guid InvitedByUserId { get; set; }

    /// <summary>
    /// UTC timestamp when the invitation was accepted (null if not yet accepted).
    /// </summary>
    public DateTime? AcceptedAt { get; set; }

    // IConcurrent interface properties
    /// <summary>
    /// Row version for optimistic concurrency control.
    /// </summary>
    [Timestamp]
    public byte[] RowVersion { get; set; } = null!;

    // IAuditable interface properties
    /// <summary>
    /// UTC timestamp when the invitation was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// User ID who created the invitation.
    /// </summary>
    public Guid CreatedBy { get; set; }

    /// <summary>
    /// UTC timestamp when the invitation was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// User ID who last updated the invitation.
    /// </summary>
    public Guid? UpdatedBy { get; set; }

    // Navigation properties
    /// <summary>
    /// Thed user (account created immediately, inactive until accepted).
    /// </summary>
    public User User { get; set; } = null!;

    /// <summary>
    /// The admin user who sent the invitation.
    /// </summary>
    public User InvitedBy { get; set; } = null!;
}
