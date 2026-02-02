using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Quater.Backend.Core.Tests.Helpers;
using Quater.Backend.Data;
using Quater.Shared.Enums;
using Quater.Shared.Models;
using Xunit;

namespace Quater.Backend.Core.Tests.Data;

/// <summary>
/// Tests for AuditTrailInterceptor using PostgreSQL TestContainers.
/// </summary>
[Collection("TestDatabase")]
public class AuditTrailInterceptorTests : IAsyncLifetime
{
    private readonly TestDbContextFactoryFixture _fixture;
    private QuaterDbContext _context = null!;

    public AuditTrailInterceptorTests(TestDbContextFactoryFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        // Reset database to clean state before each test
        await _fixture.Factory.ResetDatabaseAsync();
        _context = _fixture.Factory.CreateContext();
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
    }

    [Fact]
    public async Task SaveChanges_NewEntity_CreatesAuditLog()
    {
        // Arrange
        var labId = await GetFirstLabIdAsync();
        var sample = MockDataFactory.CreateSample(labId);

        // Act
        _context.Samples.Add(sample);
        await _context.SaveChangesAsync();

        // Assert - Query in fresh context to ensure audit logs are visible
        using var verifyContext = _fixture.Factory.CreateContext();
        var auditLogs = await verifyContext.AuditLogs.Where(a => a.EntityId == sample.Id).ToListAsync();
        auditLogs.Should().HaveCountGreaterOrEqualTo(1);

        var auditLog = auditLogs.FirstOrDefault(a => a.EntityId == sample.Id);
        auditLog.Should().NotBeNull();
        auditLog!.Action.Should().Be(AuditAction.Create);
        auditLog.EntityType.Should().Be(EntityType.Sample);
    }

    [Fact]
    public async Task SaveChanges_UpdatedEntity_CreatesAuditLog()
    {
        // Arrange
        var labId = await GetFirstLabIdAsync();
        var sample = MockDataFactory.CreateSample(labId);
        _context.Samples.Add(sample);
        await _context.SaveChangesAsync();

        // Clear existing audit logs for this test to isolate the update log
        var existingLogs = await _context.AuditLogs.Where(a => a.EntityId == sample.Id).ToListAsync();
        _context.AuditLogs.RemoveRange(existingLogs);
        await _context.SaveChangesAsync();

        // Act
        sample.CollectorName = "Updated Collector";
        _context.Samples.Update(sample);
        await _context.SaveChangesAsync();

        // Assert - Query in fresh context to ensure audit logs are visible
        using var verifyContext = _fixture.Factory.CreateContext();
        var auditLogs = await verifyContext.AuditLogs.Where(a => a.EntityId == sample.Id).ToListAsync();
        auditLogs.Should().HaveCountGreaterOrEqualTo(1);

        var auditLog = auditLogs.FirstOrDefault(a => a.Action == AuditAction.Update);
        auditLog.Should().NotBeNull();
        auditLog!.EntityType.Should().Be(EntityType.Sample);
    }

    [Fact]
    public async Task SaveChanges_DeletedEntity_CreatesAuditLog()
    {
        // Arrange
        var labId = await GetFirstLabIdAsync();
        var sample = MockDataFactory.CreateSample(labId);
        _context.Samples.Add(sample);
        await _context.SaveChangesAsync();

        // Clear existing audit logs for this test
        var existingLogs = await _context.AuditLogs.Where(a => a.EntityId == sample.Id).ToListAsync();
        _context.AuditLogs.RemoveRange(existingLogs);
        await _context.SaveChangesAsync();

        // Act
        _context.Samples.Remove(sample);
        await _context.SaveChangesAsync();

        // Assert - Query in fresh context to ensure audit logs are visible
        // Note: SoftDeleteInterceptor converts Delete to Update (sets IsDeleted=true)
        // So we expect an Update audit log, not a Delete audit log
        using var verifyContext = _fixture.Factory.CreateContext();
        var auditLogs = await verifyContext.AuditLogs.Where(a => a.EntityId == sample.Id).ToListAsync();
        auditLogs.Should().HaveCountGreaterOrEqualTo(1);

        var auditLog = auditLogs.FirstOrDefault(a => a.Action == AuditAction.Update);
        auditLog.Should().NotBeNull();
        auditLog!.EntityType.Should().Be(EntityType.Sample);
    }

    [Fact]
    public async Task SaveChanges_MultipleChanges_CreatesMultipleAuditLogs()
    {
        // Arrange
        var labId = await GetFirstLabIdAsync();
        var sample1 = MockDataFactory.CreateSample(labId);
        var sample2 = MockDataFactory.CreateSample(labId);

        // Act
        _context.Samples.AddRange(sample1, sample2);
        await _context.SaveChangesAsync();

        // Assert - Query in fresh context to ensure audit logs are visible
        using var verifyContext = _fixture.Factory.CreateContext();
        var auditLogs = await verifyContext.AuditLogs.ToListAsync();
        auditLogs.Should().HaveCountGreaterOrEqualTo(2);

        auditLogs.Should().Contain(a => a.EntityId == sample1.Id && a.Action == AuditAction.Create);
        auditLogs.Should().Contain(a => a.EntityId == sample2.Id && a.Action == AuditAction.Create);
    }

    private async Task<Guid> GetFirstLabIdAsync()
    {
        var lab = await _context.Labs.FirstOrDefaultAsync();
        if (lab != null) return lab.Id;

        // Create a lab if none exists
        var newLab = MockDataFactory.CreateLab("Test Lab for Audit");
        _context.Labs.Add(newLab);
        await _context.SaveChangesAsync();
        return newLab.Id;
    }
}
