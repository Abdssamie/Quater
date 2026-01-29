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
    public double? MoroccanThreshold { get; set; }
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
    public string Name { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public double? WhoThreshold { get; set; }
    public double? MoroccanThreshold { get; set; }
    public double? MinValue { get; set; }
    public double? MaxValue { get; set; }
    public string? Description { get; set; }
}

/// <summary>
/// DTO for updating an existing parameter
/// </summary>
public class UpdateParameterDto
{
    public string Name { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public double? WhoThreshold { get; set; }
    public double? MoroccanThreshold { get; set; }
    public double? MinValue { get; set; }
    public double? MaxValue { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}
