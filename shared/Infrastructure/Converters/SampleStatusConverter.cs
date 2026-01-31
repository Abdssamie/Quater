using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Quater.Shared.Enums;

namespace Quater.Shared.Infrastructure.Converters;

/// <summary>
/// EF Core Value Converter for SampleStatus enum to string conversion.
/// Enables SQLite compatibility while maintaining type safety in C# code.
/// </summary>
/// <remarks>
/// This converter is part of the Core Domain Pattern implementation that allows
/// both backend (PostgreSQL) and desktop (SQLite) applications to use the same
/// domain models while storing enums as strings in the database.
/// 
/// Usage in DbContext:
/// <code>
/// entity.Property(e => e.Status)
///     .HasConversion(new SampleStatusConverter());
/// </code>
/// </remarks>
public class SampleStatusConverter : ValueConverter<SampleStatus, string>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SampleStatusConverter"/> class.
    /// </summary>
    public SampleStatusConverter() 
        : base(
            v => v.ToString(),                                    // Convert enum to string
            v => Enum.Parse<SampleStatus>(v)) // Convert string to enum
    {
    }
}
