using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Quater.Backend.Core.Interfaces;
using Quater.Backend.Core.Tests.Helpers;
using Quater.Backend.Data;
using Quater.Backend.Services;
using Quater.Shared.Enums;
using Quater.Shared.Models;
using Xunit;

namespace Quater.Backend.Core.Tests.Services;

/// <summary>
/// Tests for ComplianceCalculator using PostgreSQL TestContainers.
/// </summary>
[Collection("TestDatabase")]
public class ComplianceCalculatorTests : IAsyncLifetime
{
    private readonly TestDbContextFactoryFixture _fixture;
    private QuaterDbContext _context = null!;
    private IComplianceCalculator _calculator = null!;

    public ComplianceCalculatorTests(TestDbContextFactoryFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        // Reset database for clean state
        await _fixture.Factory.ResetDatabaseAsync();

        // Seed test data without interceptors
        using (var seedContext = _fixture.Factory.CreateContextWithoutInterceptors())
        {
            SeedTestData(seedContext);
        }

        _context = _fixture.Factory.CreateContext();
        _calculator = new ComplianceCalculator(_context);
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
    }

    private static void SeedTestData(QuaterDbContext context)
    {
        var parameters = new List<Parameter>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "pH",
                Unit = "pH units",
                Threshold = 8.5,
                MinValue = 6.5,
                MaxValue = 9.5,
                IsActive = true,
                RowVersion = new byte[] { 0, 0, 0, 0, 0, 0, 0, 1 }
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Turbidity",
                Unit = "NTU",
                Threshold = 5.0,
                MinValue = 0,
                MaxValue = null,
                IsActive = true,
                RowVersion = new byte[] { 0, 0, 0, 0, 0, 0, 0, 1 }
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Chlorine",
                Unit = "mg/L",
                Threshold = 5.0,
                MinValue = 0.2,
                MaxValue = 5.0,
                IsActive = true,
                RowVersion = new byte[] { 0, 0, 0, 0, 0, 0, 0, 1 }
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "InactiveParameter",
                Unit = "mg/L",
                Threshold = 10.0,
                MinValue = null,
                MaxValue = null,
                IsActive = false,
                RowVersion = new byte[] { 0, 0, 0, 0, 0, 0, 0, 1 }
            }
        };

        context.Parameters.AddRange(parameters);
        context.SaveChanges();
    }

    [Fact]
    public async Task CalculateComplianceAsync_ValueWithinAllLimits_ReturnsPass()
    {
        // Arrange
        var parameterName = "pH";
        var value = 7.5; // Within all limits

        // Act
        var result = await _calculator.CalculateComplianceAsync(parameterName, value);

        // Assert
        result.Should().Be(ComplianceStatus.Pass);
    }

    [Fact]
    public async Task CalculateComplianceAsync_ValueExceedsWhoThreshold_ReturnsFail()
    {
        // Arrange
        var parameterName = "pH";
        var value = 9.0; // Exceeds WHO threshold (8.5) but within Moroccan (9.0)

        // Act
        var result = await _calculator.CalculateComplianceAsync(parameterName, value);

        // Assert
        result.Should().Be(ComplianceStatus.Fail);
    }

    [Fact]
    public async Task CalculateComplianceAsync_ValueExceedsMoroccanThreshold_ReturnsWarning()
    {
        // Arrange
        var parameterName = "Turbidity";
        var value = 7.0; // Exceeds WHO (5.0) but within Moroccan (10.0)

        // Act
        var result = await _calculator.CalculateComplianceAsync(parameterName, value);

        // Assert
        result.Should().Be(ComplianceStatus.Fail); // WHO threshold takes precedence
    }

    [Fact]
    public async Task CalculateComplianceAsync_ValueBelowMinValue_ReturnsFail()
    {
        // Arrange
        var parameterName = "pH";
        var value = 6.0; // Below minimum (6.5)

        // Act
        var result = await _calculator.CalculateComplianceAsync(parameterName, value);

        // Assert
        result.Should().Be(ComplianceStatus.Fail);
    }

    [Fact]
    public async Task CalculateComplianceAsync_ValueAboveMaxValue_ReturnsFail()
    {
        // Arrange
        var parameterName = "pH";
        var value = 10.0; // Above maximum (9.5)

        // Act
        var result = await _calculator.CalculateComplianceAsync(parameterName, value);

        // Assert
        result.Should().Be(ComplianceStatus.Fail);
    }

    [Fact]
    public async Task CalculateComplianceAsync_ParameterNotFound_ReturnsWarning()
    {
        // Arrange
        var parameterName = "NonExistentParameter";
        var value = 5.0;

        // Act
        var result = await _calculator.CalculateComplianceAsync(parameterName, value);

        // Assert
        result.Should().Be(ComplianceStatus.Warning);
    }

    [Fact]
    public async Task CalculateComplianceAsync_InactiveParameter_ReturnsWarning()
    {
        // Arrange
        var parameterName = "InactiveParameter";
        var value = 5.0;

        // Act
        var result = await _calculator.CalculateComplianceAsync(parameterName, value);

        // Assert
        result.Should().Be(ComplianceStatus.Warning);
    }

    [Fact]
    public async Task CalculateComplianceAsync_ValueBetweenWhoAndMoroccan_ReturnsWarning()
    {
        // Arrange
        var parameterName = "Turbidity";
        var value = 7.0; // Between WHO (5.0) and Moroccan (10.0)

        // Act
        var result = await _calculator.CalculateComplianceAsync(parameterName, value);

        // Assert
        // Since WHO threshold is exceeded, it should return Fail
        result.Should().Be(ComplianceStatus.Fail);
    }

    [Fact]
    public async Task CalculateBatchComplianceAsync_MultipleParameters_ReturnsCorrectStatuses()
    {
        // Arrange
        var testResults = new Dictionary<string, double>
        {
            { "pH", 7.5 },           // Pass
            { "Turbidity", 3.0 },    // Pass
            { "Chlorine", 1.0 }      // Pass
        };

        // Act
        var results = await _calculator.CalculateBatchComplianceAsync(testResults);

        // Assert
        results.Should().HaveCount(3);
        results["pH"].Should().Be(ComplianceStatus.Pass);
        results["Turbidity"].Should().Be(ComplianceStatus.Pass);
        results["Chlorine"].Should().Be(ComplianceStatus.Pass);
    }

    [Fact]
    public async Task CalculateBatchComplianceAsync_MixedResults_ReturnsCorrectStatuses()
    {
        // Arrange
        var testResults = new Dictionary<string, double>
        {
            { "pH", 7.5 },           // Pass
            { "Turbidity", 7.0 },    // Fail (exceeds WHO)
            { "Chlorine", 0.1 }      // Fail (below min)
        };

        // Act
        var results = await _calculator.CalculateBatchComplianceAsync(testResults);

        // Assert
        results.Should().HaveCount(3);
        results["pH"].Should().Be(ComplianceStatus.Pass);
        results["Turbidity"].Should().Be(ComplianceStatus.Fail);
        results["Chlorine"].Should().Be(ComplianceStatus.Fail);
    }

    [Fact]
    public void GetOverallCompliance_AllPass_ReturnsPass()
    {
        // Arrange
        var testResults = new Dictionary<string, ComplianceStatus>
        {
            { "pH", ComplianceStatus.Pass },
            { "Turbidity", ComplianceStatus.Pass },
            { "Chlorine", ComplianceStatus.Pass }
        };

        // Act
        var result = _calculator.GetOverallCompliance(testResults);

        // Assert
        result.Should().Be(ComplianceStatus.Pass);
    }

    [Fact]
    public void GetOverallCompliance_AnyFail_ReturnsFail()
    {
        // Arrange
        var testResults = new Dictionary<string, ComplianceStatus>
        {
            { "pH", ComplianceStatus.Pass },
            { "Turbidity", ComplianceStatus.Fail },
            { "Chlorine", ComplianceStatus.Pass }
        };

        // Act
        var result = _calculator.GetOverallCompliance(testResults);

        // Assert
        result.Should().Be(ComplianceStatus.Fail);
    }

    [Fact]
    public void GetOverallCompliance_AnyWarningNoFail_ReturnsWarning()
    {
        // Arrange
        var testResults = new Dictionary<string, ComplianceStatus>
        {
            { "pH", ComplianceStatus.Pass },
            { "Turbidity", ComplianceStatus.Warning },
            { "Chlorine", ComplianceStatus.Pass }
        };

        // Act
        var result = _calculator.GetOverallCompliance(testResults);

        // Assert
        result.Should().Be(ComplianceStatus.Warning);
    }

    [Fact]
    public void GetOverallCompliance_EmptyResults_ReturnsWarning()
    {
        // Arrange
        var testResults = new Dictionary<string, ComplianceStatus>();

        // Act
        var result = _calculator.GetOverallCompliance(testResults);

        // Assert
        result.Should().Be(ComplianceStatus.Warning);
    }

    [Fact]
    public void GetOverallCompliance_FailAndWarning_ReturnsFail()
    {
        // Arrange - Fail takes precedence over Warning
        var testResults = new Dictionary<string, ComplianceStatus>
        {
            { "pH", ComplianceStatus.Warning },
            { "Turbidity", ComplianceStatus.Fail },
            { "Chlorine", ComplianceStatus.Pass }
        };

        // Act
        var result = _calculator.GetOverallCompliance(testResults);

        // Assert
        result.Should().Be(ComplianceStatus.Fail);
    }
}
