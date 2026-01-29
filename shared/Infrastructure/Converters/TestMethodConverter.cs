using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Quater.Shared.Enums;

namespace Quater.Shared.Infrastructure.Converters;

/// <summary>
/// EF Core Value Converter for TestMethod enum to string conversion.
/// Enables SQLite compatibility while maintaining type safety in C# code.
/// Converts: TestMethod.Titration <-> "Titration"
/// </summary>
public class TestMethodConverter : ValueConverter<TestMethod, string>
{
    public TestMethodConverter() 
        : base(
            v => v.ToString(),
            v => (TestMethod)Enum.Parse(typeof(TestMethod), v))
    {
    }
}
