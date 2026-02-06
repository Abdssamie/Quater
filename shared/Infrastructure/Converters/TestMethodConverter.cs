using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Quater.Shared.Enums;

namespace Quater.Shared.Infrastructure.Converters;

/// <summary>
/// Converts TestMethod enum to/from string for database storage
/// </summary>
public class TestMethodConverter : ValueConverter<TestMethod, string>
{
    public TestMethodConverter()
        : base(
            v => v.ToString(),
            v => Enum.Parse<TestMethod>(v))
    {
    }
}
