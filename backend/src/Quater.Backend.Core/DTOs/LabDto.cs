using System.ComponentModel.DataAnnotations;

namespace Quater.Backend.Core.DTOs;

/// <summary>
/// Data Transfer Object for Lab entity
/// </summary>
public class LabDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string? ContactInfo { get; set; }
    public DateTime CreatedDate { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}

/// <summary>
/// DTO for creating a new lab
/// </summary>
public class CreateLabDto
{
    [Required(ErrorMessage = "Lab name is required")]
    [MaxLength(200, ErrorMessage = "Lab name cannot exceed 200 characters")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500, ErrorMessage = "Location cannot exceed 500 characters")]
    public string? Location { get; set; }

    [MaxLength(500, ErrorMessage = "Contact info cannot exceed 500 characters")]
    public string? ContactInfo { get; set; }
}

/// <summary>
/// DTO for updating an existing lab
/// </summary>
public class UpdateLabDto
{
    [Required(ErrorMessage = "Lab name is required")]
    [MaxLength(200, ErrorMessage = "Lab name cannot exceed 200 characters")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500, ErrorMessage = "Location cannot exceed 500 characters")]
    public string? Location { get; set; }

    [MaxLength(500, ErrorMessage = "Contact info cannot exceed 500 characters")]
    public string? ContactInfo { get; set; }

    public bool IsActive { get; set; }
}
