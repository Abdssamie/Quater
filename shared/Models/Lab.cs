using System.ComponentModel.DataAnnotations;

namespace Quater.Shared.Models;

/// <summary>
/// Represents a water quality lab organization.
/// </summary>
public class Lab
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

    // Navigation properties
    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<Sample> Samples { get; set; } = new List<Sample>();
}
