using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Quater.Backend.Core.Tests.Helpers;
using Quater.Backend.Data;
using Quater.Shared.Enums;
using Quater.Shared.Models;
using Xunit;

namespace Quater.Backend.Core.Tests.Data;

public class AuditTrailInterceptorTests : IDisposable
{
    private readonly QuaterDbContext _context;
    private readonly string _databaseName;

    public AuditTrailInterceptorTests()
    {
        _databaseName = Guid.NewGuid().ToString();
        _context = TestDbContextFactory.CreateInMemoryContext(_databaseName);
    }

    [Fact]
    public async Task SaveChanges_NewEntity_CreatesAuditLog()
    {
        // Arrange
        var sample = MockDataFactory.CreateSample();

        // Act
        _context.Samples.Add(sample);
        await _context.SaveChangesAsync();

        // Assert - Query in fresh context to ensure audit logs are visible after two-phase save
        using var verifyContext = TestDbContextFactory.CreateInMemoryContext(_databaseName);
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
        var sample = MockDataFactory.CreateSample();
        _context.Samples.Add(sample);
        await _context.SaveChangesAsync();

        // Clear existing audit logs for this test
        var existingLogs = await _context.AuditLogs.Where(a => a.EntityId == sample.Id).ToListAsync();
        _context.AuditLogs.RemoveRange(existingLogs);
        await _context.SaveChangesAsync();

        // Act
        sample.CollectorName = "Updated Collector";
        _context.Samples.Update(sample);
        await _context.SaveChangesAsync();

        // Assert - Query in fresh context to ensure audit logs are visible after two-phase save
        using var verifyContext = TestDbContextFactory.CreateInMemoryContext(_databaseName);
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
        var sample = MockDataFactory.CreateSample();
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
        using var verifyContext = TestDbContextFactory.CreateInMemoryContext(_databaseName);
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
        var sample1 = MockDataFactory.CreateSample();
        var sample2 = MockDataFactory.CreateSample();

        // Act
        _context.Samples.AddRange(sample1, sample2);
        await _context.SaveChangesAsync();

        // Assert - Query in fresh context to ensure audit logs are visible after two-phase save
        using var verifyContext = TestDbContextFactory.CreateInMemoryContext(_databaseName);
        var auditLogs = await verifyContext.AuditLogs.ToListAsync();
        auditLogs.Should().HaveCountGreaterOrEqualTo(2);
        
        auditLogs.Should().Contain(a => a.EntityId == sample1.Id && a.Action == AuditAction.Create);
        auditLogs.Should().Contain(a => a.EntityId == sample2.Id && a.Action == AuditAction.Create);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
