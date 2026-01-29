using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Quater.Shared.Enums;

namespace Quater.Shared.Infrastructure.Converters;

/// <summary>
/// EF Core Value Converter for ComplianceStatus enum to string conversion.
/// Enables SQLite compatibility while maintaining type safety in C# code.
/// </summary>
/// <remarks>
/// This converter is part of the Core Domain Pattern implementation that allows
/// both backend (PostgreSQL) and desktop (SQLite) applications to use the same
/// domain models while storing enums as strings in the database.
/// 
/// Conversion examples:
/// - ComplianceStatus.Pass <-> "Pass"
/// - ComplianceStatus.Fail <-> "Fail"
/// - ComplianceStatus.Pending <-> "Pending"
/// 
/// Usage in DbContext:
/// <code>
/// entity.Property(e => e.ComplianceStatus)
///     .HasConversion(new ComplianceStatusConverter());
/// </code>
/// </remarks>
public class ComplianceStatusConverter : ValueConverter<ComplianceStatus, string>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ComplianceStatusConverter"/> class.
    /// </summary>
    public ComplianceStatusConverter() 
        : base(
            v => v.ToString(),                                        // Convert enum to string
            v => (ComplianceStatus)Enum.Parse(typeof(ComplianceStatus), v)) // Convert string to enum
    {
    }
}
