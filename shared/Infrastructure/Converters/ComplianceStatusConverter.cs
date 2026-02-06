using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Quater.Shared.Enums;

namespace Quater.Shared.Infrastructure.Converters;

/// <summary>
/// Converts ComplianceStatus enum to/from string for database storage
/// </summary>
public class ComplianceStatusConverter : ValueConverter<ComplianceStatus, string>
{
    public ComplianceStatusConverter()
        : base(
            v => v.ToString(),
            v => Enum.Parse<ComplianceStatus>(v))
    {
    }
}
