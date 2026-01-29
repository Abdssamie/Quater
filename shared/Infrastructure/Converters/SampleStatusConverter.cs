using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Quater.Shared.Enums;

namespace Quater.Shared.Infrastructure.Converters;

/// <summary>
/// EF Core Value Converter for SampleStatus enum to string conversion.
/// Enables SQLite compatibility while maintaining type safety in C# code.
/// Converts: SampleStatus.Pending <-> "Pending"
/// </summary>
public class SampleStatusConverter : ValueConverter<SampleStatus, string>
{
    public SampleStatusConverter() 
        : base(
            v => v.ToString(),
            v => (SampleStatus)Enum.Parse(typeof(SampleStatus), v))
    {
    }
}
