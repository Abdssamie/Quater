using System.ComponentModel.DataAnnotations;
using Quater.Shared.Enums;

namespace Quater.Backend.Core.DTOs;

/// <summary>
/// Data Transfer Object for TestResult entity
/// </summary>
public class TestResultDto
{
    public Guid Id { get; set; }
    public Guid SampleId { get; set; }
    public string ParameterName { get; set; } = string.Empty;
    public double Value { get; set; }
    public string Unit { get; set; } = string.Empty;
    public DateTime TestDate { get; set; }
    public string TechnicianName { get; set; } = string.Empty;
    public TestMethod TestMethod { get; set; }
    public ComplianceStatus ComplianceStatus { get; set; }
    public int Version { get; set; }
    public DateTime LastModified { get; set; }
    public Guid LastModifiedBy { get; set; }
    public bool IsDeleted { get; set; }
    public bool IsSynced { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTime CreatedDate { get; set; }
}

/// <summary>
/// DTO for creating a new test result
/// </summary>
public class CreateTestResultDto
{
    [Required(ErrorMessage = "Sample ID is required")]
    public Guid SampleId { get; set; }

    [Required(ErrorMessage = "Parameter name is required")]
    [MaxLength(100, ErrorMessage = "Parameter name cannot exceed 100 characters")]
    public string ParameterName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Value is required")]
    public double Value { get; set; }

    [Required(ErrorMessage = "Unit is required")]
    [MaxLength(20, ErrorMessage = "Unit cannot exceed 20 characters")]
    public string Unit { get; set; } = string.Empty;

    [Required(ErrorMessage = "Test date is required")]
    public DateTime TestDate { get; set; }

    [Required(ErrorMessage = "Technician name is required")]
    [MaxLength(100, ErrorMessage = "Technician name cannot exceed 100 characters")]
    public string TechnicianName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Test method is required")]
    public TestMethod TestMethod { get; set; }
}

/// <summary>
/// DTO for updating an existing test result
/// </summary>
public class UpdateTestResultDto
{
    [Required(ErrorMessage = "Parameter name is required")]
    [MaxLength(100, ErrorMessage = "Parameter name cannot exceed 100 characters")]
    public string ParameterName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Value is required")]
    public double Value { get; set; }

    [Required(ErrorMessage = "Unit is required")]
    [MaxLength(20, ErrorMessage = "Unit cannot exceed 20 characters")]
    public string Unit { get; set; } = string.Empty;

    [Required(ErrorMessage = "Test date is required")]
    public DateTime TestDate { get; set; }

    [Required(ErrorMessage = "Technician name is required")]
    [MaxLength(100, ErrorMessage = "Technician name cannot exceed 100 characters")]
    public string TechnicianName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Test method is required")]
    public TestMethod TestMethod { get; set; }

    [Required(ErrorMessage = "Version is required")]
    public int Version { get; set; }
}
