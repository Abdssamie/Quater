using Quater.Shared.Models;

namespace Quater.Shared.ValueObjects;

/// <summary>
/// Represents a water quality measurement with validated parameter/unit combination.
/// Prevents invalid parameter/unit combinations and enforces value ranges.
/// </summary>
public sealed record Measurement
{
    /// <summary>
    /// Reference to the Parameter entity defining valid units and ranges
    /// </summary>
    public Guid ParameterId { get; init; }

    /// <summary>
    /// Measured value
    /// </summary>
    public double Value { get; init; }

    /// <summary>
    /// Unit of measurement (must match Parameter definition)
    /// </summary>
    public string Unit { get; init; }

    /// <summary>
    /// Creates a new Measurement with validation against Parameter definition.
    /// </summary>
    /// <param name="parameter">Parameter entity with validation rules</param>
    /// <param name="value">Measured value</param>
    /// <param name="unit">Unit of measurement</param>
    /// <exception cref="ArgumentNullException">If parameter or unit is null</exception>
    /// <exception cref="ArgumentException">If unit doesn't match parameter or value out of range</exception>
    public Measurement(Parameter parameter, double value, string unit)
    {
        ArgumentNullException.ThrowIfNull(parameter);
        ArgumentException.ThrowIfNullOrWhiteSpace(unit);

        // Validate unit matches parameter
        if (!string.Equals(parameter.Unit, unit, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException(
                $"Unit '{unit}' does not match parameter '{parameter.Name}' expected unit '{parameter.Unit}'",
                nameof(unit));

        // Validate value range
        if (parameter.MinValue.HasValue && value < parameter.MinValue.Value)
            throw new ArgumentOutOfRangeException(nameof(value), value,
                $"Value {value} is below minimum {parameter.MinValue.Value} for parameter '{parameter.Name}'");

        if (parameter.MaxValue.HasValue && value > parameter.MaxValue.Value)
            throw new ArgumentOutOfRangeException(nameof(value), value,
                $"Value {value} exceeds maximum {parameter.MaxValue.Value} for parameter '{parameter.Name}'");

        ParameterId = parameter.Id;
        Value = value;
        Unit = unit;
    }

    /// <summary>
    /// Creates a Measurement from existing data (for deserialization/EF Core).
    /// Does not validate against Parameter entity.
    /// </summary>
    /// <param name="parameterId">Parameter identifier</param>
    /// <param name="value">Measured value</param>
    /// <param name="unit">Unit of measurement</param>
    public Measurement(Guid parameterId, double value, string unit)
    {
        ParameterId = parameterId;
        Value = value;
        Unit = unit ?? throw new ArgumentNullException(nameof(unit));
    }
}
