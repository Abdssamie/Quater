using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Quater.Shared.Enums;

namespace Quater.Shared.Infrastructure.Converters;

/// <summary>
/// EF Core Value Converter for SampleType enum to string conversion.
/// Enables SQLite compatibility while maintaining type safety in C# code.
/// </summary>
/// <remarks>
/// This converter is part of the Core Domain Pattern implementation that allows
/// both backend (PostgreSQL) and desktop (SQLite) applications to use the same
/// domain models while storing enums as strings in the database.
/// 
/// Conversion examples:
/// - SampleType.DrinkingWater = "DrinkingWater"
/// - SampleType.Wastewater = "Wastewater"
/// - SampleType.SurfaceWater = "SurfaceWater"
/// 
/// Usage in DbContext:
/// <code>
/// entity.Property(e => e.Type)
///     .HasConversion(new SampleTypeConverter());
/// </code>
/// </remarks>
public class SampleTypeConverter : ValueConverter<SampleType, string>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SampleTypeConverter"/> class.
    /// </summary>
    public SampleTypeConverter()
        : base(
            v => v.ToString(),             // Convert enum to string
            v => Enum.Parse<SampleType>(v)) // Convert string to enum
    {
    }
}
