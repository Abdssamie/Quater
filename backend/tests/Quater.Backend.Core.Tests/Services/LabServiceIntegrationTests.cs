using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Quater.Backend.Core.Constants;
using Quater.Backend.Core.DTOs;
using Quater.Backend.Core.Tests.Helpers;
using Quater.Backend.Data;
using Quater.Backend.Services;
using Quater.Shared.Models;
using Xunit;

namespace Quater.Backend.Core.Tests.Services;

/// <summary>
/// Integration tests for LabService using PostgreSQL test container
/// </summary>
[Collection("PostgreSQL")]
public class LabServiceIntegrationTests : IAsyncLifetime
{
    private readonly PostgresTestContainerFixture _fixture;
    private QuaterDbContext _context = null!;
    private FakeTimeProvider _timeProvider = null!;
    private LabService _service = null!;

    public LabServiceIntegrationTests(PostgresTestContainerFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        // Reset database before each test
        await _fixture.Container.ResetDatabaseAsync();
        
        _context = _fixture.Container.CreateDbContext();
        _timeProvider = new FakeTimeProvider();
        
        // LabService doesn't have a specific validator injected based on typical pattern, 
        // but let's check the constructor to be sure.
        // Assuming (QuaterDbContext context, TimeProvider timeProvider)
        _service = new LabService(_context);
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
    }

    [Fact]
    public async Task CreateAsync_ValidLab_CreatesLab()
    {
        // Arrange
        var dto = new CreateLabDto
        {
            Name = "New Test Lab",
            Location = "123 Test St",
            ContactInfo = "test@lab.com"
        };

        // Act
        var result = await _service.CreateAsync(dto, Guid.NewGuid());

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBeEmpty();
        result.Name.Should().Be(dto.Name);
        result.IsActive.Should().BeTrue();

        // Verify persistence
        var persisted = await _context.Labs.FindAsync(result.Id);
        persisted.Should().NotBeNull();
        persisted!.Name.Should().Be(dto.Name);
        persisted.CreatedBy.Should().Be(SystemUser.GetId()); // Set by AuditInterceptor (no ICurrentUserService in tests)
    }

    [Fact]
    public async Task GetByIdAsync_ExistingLab_ReturnsLab()
    {
        // Arrange
        var lab = new Lab 
        { 
            Id = Guid.NewGuid(), 
            Name = "Existing Lab", 
            IsActive = true,
        };
        _context.Labs.Add(lab);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetByIdAsync(lab.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(lab.Id);
        result.Name.Should().Be(lab.Name);
    }

    [Fact]
    public async Task UpdateAsync_ExistingLab_UpdatesLab()
    {
        // Arrange
        var lab = new Lab 
        { 
            Id = Guid.NewGuid(), 
            Name = "Original Name", 
            IsActive = true,
        };
        _context.Labs.Add(lab);
        await _context.SaveChangesAsync();

        var dto = new UpdateLabDto
        {
            Name = "Updated Name",
            Location = "Updated Location",
            ContactInfo = "updated@lab.com",
            IsActive = false
        };

        // Act
        var result = await _service.UpdateAsync(lab.Id, dto, Guid.NewGuid());

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be(dto.Name);
        result.IsActive.Should().BeFalse();

        // Verify persistence
        var persisted = await _context.Labs.FindAsync(lab.Id);
        persisted!.Name.Should().Be(dto.Name);
        persisted.UpdatedBy.Should().Be(SystemUser.GetId()); // Set by AuditInterceptor (no ICurrentUserService in tests)
    }

    [Fact]
    public async Task DeleteAsync_ExistingLab_SoftDeletesLab()
    {
        // Arrange
        var lab = new Lab 
        { 
            Id = Guid.NewGuid(), 
            Name = "To Delete", 
            IsActive = true,
        };
        _context.Labs.Add(lab);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.DeleteAsync(lab.Id);

        // Assert
        result.Should().BeTrue();

        // Verify soft delete
        var persisted = await _context.Labs.IgnoreQueryFilters().FirstOrDefaultAsync(l => l.Id == lab.Id);
        persisted.Should().NotBeNull();
        persisted!.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsPagedResults()
    {
        // Arrange
        var labs = new List<Lab>
        {
            new() { Id = Guid.NewGuid(), Name = "Lab 1", IsActive = true },
            new() { Id = Guid.NewGuid(), Name = "Lab 2", IsActive = true },
            new() { Id = Guid.NewGuid(), Name = "Lab 3", IsActive = true }
        };
        _context.Labs.AddRange(labs);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetAllAsync(pageNumber: 1, pageSize: 2);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        // TotalCount is 3 created labs + 1 system lab = 4
        result.TotalCount.Should().Be(4);
    }
}
