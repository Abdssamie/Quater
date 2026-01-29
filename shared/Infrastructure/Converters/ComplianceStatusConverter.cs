using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Quater.Shared.Enums;

namespace Quater.Shared.Infrastructure.Converters;

/// <summary>
/// EF Core Value Converter for ComplianceStatus enum to string conversion.
/// Enables SQLite compatibility while maintaining type safety in C# code.
/// Converts: ComplianceStatus.Pass <-> "Pass"
/// </summary>
public class ComplianceStatusConverter : ValueConverter<ComplianceStatus, string>
{
    public ComplianceStatusConverter() 
        : base(
            v => v.ToString(),
            v => (ComplianceStatus)Enum.Parse(typeof(ComplianceStatus), v))
    {
    }
}
