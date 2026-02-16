using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Quater.Shared.Enums;

namespace Quater.Shared.Infrastructure.Converters;

/// <summary>
/// Converts AuditAction enum to/from string for database storage
/// </summary>
public class AuditActionConverter : ValueConverter<AuditAction, string>
{
    public AuditActionConverter()
        : base(
            v => v.ToString(),
            v => Enum.Parse<AuditAction>(v))
    {
    }
}
