using FluentAssertions;
using Quater.Shared.Enums;
using Quater.Shared.Infrastructure.Converters;
using Xunit;

namespace Quater.Backend.Core.Tests.Infrastructure;

public class ConverterTests
{
    [Theory]
    [InlineData(SampleType.DrinkingWater, "DrinkingWater")]
    [InlineData(SampleType.Wastewater, "Wastewater")]
    [InlineData(SampleType.SurfaceWater, "SurfaceWater")]
    [InlineData(SampleType.Groundwater, "Groundwater")]
    [InlineData(SampleType.IndustrialWater, "IndustrialWater")]
    public void SampleTypeConverter_ShouldConvertEnumToString(SampleType enumValue, string expectedString)
    {
        // Arrange
        var converter = new SampleTypeConverter();

        // Act
        var result = converter.ConvertToProvider(enumValue);

        // Assert
        result.Should().Be(expectedString);
    }

    [Theory]
    [InlineData("DrinkingWater", SampleType.DrinkingWater)]
    [InlineData("Wastewater", SampleType.Wastewater)]
    [InlineData("SurfaceWater", SampleType.SurfaceWater)]
    [InlineData("Groundwater", SampleType.Groundwater)]
    [InlineData("IndustrialWater", SampleType.IndustrialWater)]
    public void SampleTypeConverter_ShouldConvertStringToEnum(string stringValue, SampleType expectedEnum)
    {
        // Arrange
        var converter = new SampleTypeConverter();

        // Act
        var result = converter.ConvertFromProvider(stringValue);

        // Assert
        result.Should().Be(expectedEnum);
    }

    [Theory]
    [InlineData(SampleStatus.Pending, "Pending")]
    [InlineData(SampleStatus.Completed, "Completed")]
    [InlineData(SampleStatus.Archived, "Archived")]
    public void SampleStatusConverter_ShouldConvertEnumToString(SampleStatus enumValue, string expectedString)
    {
        // Arrange
        var converter = new SampleStatusConverter();

        // Act
        var result = converter.ConvertToProvider(enumValue);

        // Assert
        result.Should().Be(expectedString);
    }

    [Theory]
    [InlineData("Pending", SampleStatus.Pending)]
    [InlineData("Completed", SampleStatus.Completed)]
    [InlineData("Archived", SampleStatus.Archived)]
    public void SampleStatusConverter_ShouldConvertStringToEnum(string stringValue, SampleStatus expectedEnum)
    {
        // Arrange
        var converter = new SampleStatusConverter();

        // Act
        var result = converter.ConvertFromProvider(stringValue);

        // Assert
        result.Should().Be(expectedEnum);
    }

    [Theory]
    [InlineData(TestMethod.Titration, "Titration")]
    [InlineData(TestMethod.Spectrophotometry, "Spectrophotometry")]
    [InlineData(TestMethod.Chromatography, "Chromatography")]
    [InlineData(TestMethod.Microscopy, "Microscopy")]
    [InlineData(TestMethod.Electrode, "Electrode")]
    [InlineData(TestMethod.Culture, "Culture")]
    [InlineData(TestMethod.Other, "Other")]
    public void TestMethodConverter_ShouldConvertEnumToString(TestMethod enumValue, string expectedString)
    {
        // Arrange
        var converter = new TestMethodConverter();

        // Act
        var result = converter.ConvertToProvider(enumValue);

        // Assert
        result.Should().Be(expectedString);
    }

    [Theory]
    [InlineData("Titration", TestMethod.Titration)]
    [InlineData("Spectrophotometry", TestMethod.Spectrophotometry)]
    [InlineData("Chromatography", TestMethod.Chromatography)]
    [InlineData("Microscopy", TestMethod.Microscopy)]
    [InlineData("Electrode", TestMethod.Electrode)]
    [InlineData("Culture", TestMethod.Culture)]
    [InlineData("Other", TestMethod.Other)]
    public void TestMethodConverter_ShouldConvertStringToEnum(string stringValue, TestMethod expectedEnum)
    {
        // Arrange
        var converter = new TestMethodConverter();

        // Act
        var result = converter.ConvertFromProvider(stringValue);

        // Assert
        result.Should().Be(expectedEnum);
    }

    [Theory]
    [InlineData(ComplianceStatus.Pass, "Pass")]
    [InlineData(ComplianceStatus.Fail, "Fail")]
    [InlineData(ComplianceStatus.Warning, "Warning")]
    public void ComplianceStatusConverter_ShouldConvertEnumToString(ComplianceStatus enumValue, string expectedString)
    {
        // Arrange
        var converter = new ComplianceStatusConverter();

        // Act
        var result = converter.ConvertToProvider(enumValue);

        // Assert
        result.Should().Be(expectedString);
    }

    [Theory]
    [InlineData("Pass", ComplianceStatus.Pass)]
    [InlineData("Fail", ComplianceStatus.Fail)]
    [InlineData("Warning", ComplianceStatus.Warning)]
    public void ComplianceStatusConverter_ShouldConvertStringToEnum(string stringValue, ComplianceStatus expectedEnum)
    {
        // Arrange
        var converter = new ComplianceStatusConverter();

        // Act
        var result = converter.ConvertFromProvider(stringValue);

        // Assert
        result.Should().Be(expectedEnum);
    }

    [Theory]
    [InlineData(UserRole.Admin, "Admin")]
    [InlineData(UserRole.Technician, "Technician")]
    [InlineData(UserRole.Viewer, "Viewer")]
    public void UserRoleConverter_ShouldConvertEnumToString(UserRole enumValue, string expectedString)
    {
        // Arrange
        var converter = new UserRoleConverter();

        // Act
        var result = converter.ConvertToProvider(enumValue);

        // Assert
        result.Should().Be(expectedString);
    }

    [Theory]
    [InlineData("Admin", UserRole.Admin)]
    [InlineData("Technician", UserRole.Technician)]
    [InlineData("Viewer", UserRole.Viewer)]
    public void UserRoleConverter_ShouldConvertStringToEnum(string stringValue, UserRole expectedEnum)
    {
        // Arrange
        var converter = new UserRoleConverter();

        // Act
        var result = converter.ConvertFromProvider(stringValue);

        // Assert
        result.Should().Be(expectedEnum);
    }

    [Fact]
    public void SampleTypeConverter_ShouldPerformRoundTripConversion()
    {
        // Arrange
        var converter = new SampleTypeConverter();
        var originalValue = SampleType.DrinkingWater;

        // Act
        var stringValue = converter.ConvertToProvider(originalValue);
        var roundTripValue = converter.ConvertFromProvider(stringValue);

        // Assert
        roundTripValue.Should().Be(originalValue);
    }

    [Fact]
    public void SampleStatusConverter_ShouldPerformRoundTripConversion()
    {
        // Arrange
        var converter = new SampleStatusConverter();
        var originalValue = SampleStatus.Pending;

        // Act
        var stringValue = converter.ConvertToProvider(originalValue);
        var roundTripValue = converter.ConvertFromProvider(stringValue);
        // Assert
        roundTripValue.Should().Be(originalValue);
    }

    [Fact]
    public void TestMethodConverter_ShouldPerformRoundTripConversion()
    {
        // Arrange
        var converter = new TestMethodConverter();
        var originalValue = TestMethod.Spectrophotometry;

        // Act
        var stringValue = converter.ConvertToProvider(originalValue);
        var roundTripValue = converter.ConvertFromProvider(stringValue);

        // Assert
        roundTripValue.Should().Be(originalValue);
    }

    [Fact]
    public void ComplianceStatusConverter_ShouldPerformRoundTripConversion()
    {
        // Arrange
        var converter = new ComplianceStatusConverter();
        var originalValue = ComplianceStatus.Pass;

        // Act
        var stringValue = converter.ConvertToProvider(originalValue);
        var roundTripValue = converter.ConvertFromProvider(stringValue);

        // Assert
        roundTripValue.Should().Be(originalValue);
    }

    [Fact]
    public void UserRoleConverter_ShouldPerformRoundTripConversion()
    {
        // Arrange
        var converter = new UserRoleConverter();
        var originalValue = UserRole.Technician;

        // Act
        var stringValue = converter.ConvertToProvider(originalValue);
        var roundTripValue = converter.ConvertFromProvider(stringValue);

        // Assert
        roundTripValue.Should().Be(originalValue);
    }
}
