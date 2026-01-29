using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Quater.Shared.Enums;

namespace Quater.Shared.Infrastructure.Converters;

/// <summary>
/// EF Core Value Converter for SampleType enum to string conversion.
/// Enables SQLite compatibility while maintaining type safety in C# code.
/// Converts: SampleType.DrinkingWater <-> "DrinkingWater"
/// </summary>
public class SampleTypeConverter : ValueConverter<SampleType, string>
{
    public SampleTypeConverter() 
        : base(
            v => v.ToString(),
            v => (SampleType)Enum.Parse(typeof(SampleType), v))
    {
    }
}
