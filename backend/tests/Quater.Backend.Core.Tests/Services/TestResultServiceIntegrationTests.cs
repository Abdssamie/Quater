using FluentAssertions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Quater.Backend.Core.DTOs;
using Quater.Backend.Core.Exceptions;
using Quater.Backend.Core.Tests.Helpers;
using Quater.Backend.Core.Validators;
using Quater.Backend.Data;
using Quater.Backend.Services;
using Quater.Shared.Enums;
using Quater.Shared.Models;
using Xunit;

namespace Quater.Backend.Core.Tests.Services;

/// <summary>
/// Integration tests for TestResultService using PostgreSQL test container
/// </summary>
[Collection("PostgreSQL")]
public class TestResultServiceIntegrationTests : IAsyncLifetime
{
    private readonly PostgresTestContainerFixture _fixture;
    private QuaterDbContext _context = null!;
    private FakeTimeProvider _timeProvider = null!;
    private TestResultService _service = null!;

    public TestResultServiceIntegrationTests(PostgresTestContainerFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        // Reset database before each test
        await _fixture.Container.ResetDatabaseAsync();
        
        _context = _fixture.Container.CreateSeededDbContext();
        _timeProvider = new FakeTimeProvider();
        var validator = new TestResultValidator(_timeProvider);
        _service = new TestResultService(_context, validator);
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
    }

    [Fact]
    public async Task CreateAsync_ValidResult_CreatesTestResult_AndCalculatesCompliance()
    {
        // Arrange
        var sample = _context.Samples.First();
        var parameter = _context.Parameters.First(p => p.Name == "pH"); // Assuming "pH" is seeded by MockDataFactory
        
        var dto = new CreateTestResultDto
        {
            SampleId = sample.Id,
            ParameterName = parameter.Name,
            Value = 7.5,
            Unit = parameter.Unit, // Use parameter's unit
            TechnicianName = "Tech 1",
            TestMethod = TestMethod.Electrode,
            TestDate = _timeProvider.GetUtcNow().UtcDateTime
        };

        // Act
        var result = await _service.CreateAsync(dto, "test-tech");

        // Assert
        result.Should().NotBeNull();
        result.SampleId.Should().Be(sample.Id);
        result.ParameterName.Should().Be(parameter.Name);
        result.Value.Should().Be(7.5);
        result.ComplianceStatus.Should().Be(ComplianceStatus.Pass); // 7.5 is usually good for pH

        // Verify persistence
        var persisted = await _context.TestResults.FindAsync(result.Id);
        persisted.Should().NotBeNull();
        persisted!.Measurement.Value.Should().Be(7.5);
    }

    [Fact]
    public async Task CreateAsync_ExceedsThreshold_SetsComplianceStatusToFail()
    {
        // Arrange
        var sample = _context.Samples.First();
        // Assuming MockDataFactory seeds "Turbidity" with MaxValue=5
        var parameter = _context.Parameters.First(p => p.Name == "Turbidity"); 
        
        var dto = new CreateTestResultDto
        {
            SampleId = sample.Id,
            ParameterName = parameter.Name,
            Value = 10.0, // Exceeds MaxValue
            Unit = parameter.Unit,
            TechnicianName = "Tech 2",
            TestMethod = TestMethod.Spectrophotometry,
            TestDate = _timeProvider.GetUtcNow().UtcDateTime
        };

        // Act
        var result = await _service.CreateAsync(dto, "test-tech");

        // Assert
        result.ComplianceStatus.Should().Be(ComplianceStatus.Fail);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingResult_ReturnsResult()
    {
        // Arrange
        var existing = _context.TestResults.First();
        // Need to load parameter name to verify
        var paramId = existing.Measurement.ParameterId;
        var paramName = _context.Parameters.Find(paramId)!.Name;

        // Act
        var result = await _service.GetByIdAsync(existing.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(existing.Id);
        result.ParameterName.Should().Be(paramName);
    }

    [Fact]
    public async Task GetBySampleIdAsync_ReturnsResultsForSample()
    {
        // Arrange
        var sample = _context.Samples.First();
        var count = _context.TestResults.Count(tr => tr.SampleId == sample.Id);

        // Act
        var result = await _service.GetBySampleIdAsync(sample.Id);

        // Assert
        result.Items.Should().HaveCount(count);
        result.Items.All(tr => tr.SampleId == sample.Id).Should().BeTrue();
    }

    [Fact]
    public async Task UpdateAsync_ValidUpdate_UpdatesValue()
    {
        // Arrange
        var existing = _context.TestResults.First();
        var parameter = _context.Parameters.Find(existing.Measurement.ParameterId)!;
        
        // Use a value within valid range
        var newValue = 7.0; // Safe pH value

        var dto = new UpdateTestResultDto
        {
            ParameterName = parameter.Name,
            Value = newValue,
            Unit = parameter.Unit,
            TechnicianName = "Updated Tech",
            TestMethod = TestMethod.Electrode,
            TestDate = existing.TestDate,
            ComplianceStatus = ComplianceStatus.Pass,
            Version = 1
        };

        // Act
        var result = await _service.UpdateAsync(existing.Id, dto, "test-tech");

        // Assert
        result.Should().NotBeNull();
        result!.Value.Should().Be(newValue);
    }

    [Fact]
    public async Task DeleteAsync_ExistingResult_SoftDeletesResult()
    {
        // Arrange
        var existing = _context.TestResults.First();

        // Act
        var result = await _service.DeleteAsync(existing.Id);

        // Assert
        result.Should().BeTrue();

        // Verify soft delete
        var deleted = await _context.TestResults.IgnoreQueryFilters().FirstOrDefaultAsync(tr => tr.Id == existing.Id);
        deleted.Should().NotBeNull();
        deleted!.IsDeleted.Should().BeTrue();
    }
}
