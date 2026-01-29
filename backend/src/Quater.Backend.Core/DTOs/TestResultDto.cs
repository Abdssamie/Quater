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
    public string LastModifiedBy { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }
    public bool IsSynced { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
}

/// <summary>
/// DTO for creating a new test result
/// </summary>
public class CreateTestResultDto
{
    public Guid SampleId { get; set; }
    public string ParameterName { get; set; } = string.Empty;
    public double Value { get; set; }
    public string Unit { get; set; } = string.Empty;
    public DateTime TestDate { get; set; }
    public string TechnicianName { get; set; } = string.Empty;
    public TestMethod TestMethod { get; set; }
}

/// <summary>
/// DTO for updating an existing test result
/// </summary>
public class UpdateTestResultDto
{
    public string ParameterName { get; set; } = string.Empty;
    public double Value { get; set; }
    public string Unit { get; set; } = string.Empty;
    public DateTime TestDate { get; set; }
    public string TechnicianName { get; set; } = string.Empty;
    public TestMethod TestMethod { get; set; }
    public ComplianceStatus ComplianceStatus { get; set; }
    public int Version { get; set; }
}
