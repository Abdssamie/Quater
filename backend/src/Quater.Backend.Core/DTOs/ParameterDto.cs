using System.ComponentModel.DataAnnotations;

namespace Quater.Backend.Core.DTOs;

/// <summary>
/// Data Transfer Object for Parameter entity
/// </summary>
public class ParameterDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public double? WhoThreshold { get; set; }
    public double? MinValue { get; set; }
    public double? MaxValue { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime LastModified { get; set; }
}

/// <summary>
/// DTO for creating a new parameter
/// </summary>
public class CreateParameterDto
{
    [Required(ErrorMessage = "Parameter name is required")]
    [MaxLength(100, ErrorMessage = "Parameter name cannot exceed 100 characters")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Unit is required")]
    [MaxLength(20, ErrorMessage = "Unit cannot exceed 20 characters")]
    public string Unit { get; set; } = string.Empty;

    [Range(0, double.MaxValue, ErrorMessage = "WHO threshold must be non-negative")]
    public double? WhoThreshold { get; set; }

    public double? MinValue { get; set; }
    public double? MaxValue { get; set; }

    [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string? Description { get; set; }
}

/// <summary>
/// DTO for updating an existing parameter
/// </summary>
public class UpdateParameterDto
{
    [Required(ErrorMessage = "Parameter name is required")]
    [MaxLength(100, ErrorMessage = "Parameter name cannot exceed 100 characters")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Unit is required")]
    [MaxLength(20, ErrorMessage = "Unit cannot exceed 20 characters")]
    public string Unit { get; set; } = string.Empty;

    [Range(0, double.MaxValue, ErrorMessage = "WHO threshold must be non-negative")]
    public double? WhoThreshold { get; set; }

    public double? MinValue { get; set; }
    public double? MaxValue { get; set; }

    [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string? Description { get; set; }

    public bool IsActive { get; set; }
}
