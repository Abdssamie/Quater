using System.ComponentModel.DataAnnotations;
using Quater.Shared.Interfaces;

namespace Quater.Shared.Models;

/// <summary>
/// Represents a water quality lab organization.
/// </summary>
public class Lab : IEntity, IAuditable, ISoftDelete, IConcurrent
{
    /// <summary>
    /// Unique identifier (UUID)
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Lab name
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Lab physical address
    /// </summary>
    [MaxLength(500)]
    public string? Location { get; set; }

    /// <summary>
    /// Contact information (phone, email)
    /// </summary>
    [MaxLength(500)]
    public string? ContactInfo { get; set; }

    /// <summary>
    /// UTC timestamp of lab creation
    /// </summary>
    [Required]
    public DateTime CreatedDate { get; set; }

    /// <summary>
    /// Whether lab is active
    /// </summary>
    [Required]
    public bool IsActive { get; set; } = true;

    // IAuditable interface properties
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

    // ISoftDelete interface properties
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    // IConcurrent interface properties
    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    // Navigation properties
    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<Sample> Samples { get; set; } = new List<Sample>();
}
