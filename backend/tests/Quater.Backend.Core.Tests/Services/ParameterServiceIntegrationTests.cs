using FluentAssertions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Quater.Backend.Core.DTOs;
using Quater.Backend.Core.Tests.Helpers;
using Quater.Backend.Core.Validators;
using Quater.Backend.Data;
using Quater.Backend.Services;
using Quater.Shared.Models;
using Xunit;

namespace Quater.Backend.Core.Tests.Services;

/// <summary>
/// Integration tests for ParameterService using PostgreSQL test container
/// </summary>
[Collection("PostgreSQL")]
public class ParameterServiceIntegrationTests : IAsyncLifetime
{
    private readonly PostgresTestContainerFixture _fixture;
    private QuaterDbContext _context = null!;
    private FakeTimeProvider _timeProvider = null!;
    private ParameterService _service = null!;

    public ParameterServiceIntegrationTests(PostgresTestContainerFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        // Reset database before each test
        await _fixture.Container.ResetDatabaseAsync();

        _context = _fixture.Container.CreateDbContext();
        _timeProvider = new FakeTimeProvider();
        var validator = new ParameterValidator();

        _service = new ParameterService(_context, validator);
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
    }

    [Fact]
    public async Task CreateAsync_ValidParameter_CreatesParameter()
    {
        // Arrange
        var dto = new CreateParameterDto
        {
            Name = "pH",
            Unit = "pH units",
            MinValue = 0,
            MaxValue = 14,
            Description = "Acidity/Alkalinity"
        };

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBeEmpty();
        result.Name.Should().Be(dto.Name);
        result.Unit.Should().Be(dto.Unit);
        result.IsActive.Should().BeTrue();

        // Verify persistence
        var persisted = await _context.Parameters.FindAsync(result.Id);
        persisted.Should().NotBeNull();
        persisted!.Name.Should().Be(dto.Name);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingParameter_ReturnsParameter()
    {
        // Arrange
        var parameter = new Parameter
        {
            Id = Guid.NewGuid(),
            Name = "Turbidity",
            Unit = "NTU",
            IsActive = true,
        };
        _context.Parameters.Add(parameter);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetByIdAsync(parameter.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(parameter.Id);
        result.Name.Should().Be(parameter.Name);
    }

    [Fact]
    public async Task GetByNameAsync_ExistingParameter_ReturnsParameter()
    {
        // Arrange
        var parameter = new Parameter
        {
            Id = Guid.NewGuid(),
            Name = "Chlorine",
            Unit = "mg/L",
            IsActive = true,
        };
        _context.Parameters.Add(parameter);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetByNameAsync("Chlorine");

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Chlorine");
    }

    [Fact]
    public async Task UpdateAsync_ExistingParameter_UpdatesParameter()
    {
        // Arrange
        var parameter = new Parameter
        {
            Id = Guid.NewGuid(),
            Name = "Temp",
            Unit = "C",
            IsActive = true,
        };
        _context.Parameters.Add(parameter);
        await _context.SaveChangesAsync();

        var dto = new UpdateParameterDto
        {
            Name = "Temperature",
            Unit = "Celsius",
            MinValue = -10,
            MaxValue = 50,
            IsActive = true
        };

        // Act
        var result = await _service.UpdateAsync(parameter.Id, dto);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be(dto.Name);
        result.Unit.Should().Be(dto.Unit);

        // Verify persistence
        var persisted = await _context.Parameters.FindAsync(parameter.Id);
        persisted!.Name.Should().Be(dto.Name);
        persisted.Unit.Should().Be(dto.Unit);
    }

    [Fact]
    public async Task DeleteAsync_ExistingParameter_SoftDeletesParameter()
    {
        // Arrange
        var parameter = new Parameter
        {
            Id = Guid.NewGuid(),
            Name = "To Delete",
            Unit = "X",
            IsActive = true,
        };
        _context.Parameters.Add(parameter);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.DeleteAsync(parameter.Id);

        // Assert
        result.Should().BeTrue();

        // Verify soft delete
        var persisted = await _context.Parameters.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == parameter.Id);
        persisted.Should().NotBeNull();
        persisted!.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsPagedResults()
    {
        // Arrange
        var parameters = new List<Parameter>
        {
            new() { Id = Guid.NewGuid(), Name = "Param 1", Unit = "U1", IsActive = true },
            new() { Id = Guid.NewGuid(), Name = "Param 2", Unit = "U2", IsActive = true },
            new() { Id = Guid.NewGuid(), Name = "Param 3", Unit = "U3", IsActive = true }
        };
        _context.Parameters.AddRange(parameters);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetAllAsync(pageNumber: 1, pageSize: 2);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        // Note: TotalCount might vary depending on whether seeded system data exists
        result.TotalCount.Should().BeGreaterThanOrEqualTo(3);
    }
}
