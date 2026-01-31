using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Quater.Backend.Core.Tests.Helpers;
using Quater.Backend.Data;
using Quater.Shared.Enums;
using Quater.Shared.Models;
using Xunit;

namespace Quater.Backend.Core.Tests.Data;

/// <summary>
/// Integration tests for interceptors using PostgreSQL test container
/// </summary>
[Collection("PostgreSQL")]
public class InterceptorIntegrationTests : IAsyncLifetime
{
    private readonly PostgresTestContainerFixture _fixture;
    private QuaterDbContext _context = null!;

    public InterceptorIntegrationTests(PostgresTestContainerFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.Container.ResetDatabaseAsync();
        _context = _fixture.Container.CreateSeededDbContext();
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
    }

    [Fact]
    public async Task SoftDelete_DeletedEntity_SetsIsDeletedFlag()
    {
        // Arrange
        var sample = MockDataFactory.CreateSample(_context.Labs.First().Id);
        _context.Samples.Add(sample);
        await _context.SaveChangesAsync();

        // Act
        _context.Samples.Remove(sample);
        await _context.SaveChangesAsync();

        // Assert - Entity should still exist but marked as deleted
        var deletedSample = await _context.Samples
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.Id == sample.Id);
        
        deletedSample.Should().NotBeNull();
        deletedSample!.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task SoftDelete_Query_ExcludesDeletedEntities()
    {
        // Arrange
        var lab = _context.Labs.First();
        var sample1 = MockDataFactory.CreateSample(lab.Id);
        var sample2 = MockDataFactory.CreateSample(lab.Id);
        sample2.IsDeleted = true;
        
        _context.Samples.AddRange(sample1, sample2);
        await _context.SaveChangesAsync();

        // Act
        var samples = await _context.Samples.ToListAsync();

        // Assert - Only non-deleted entities should be returned
        samples.Should().Contain(s => s.Id == sample1.Id);
        samples.Should().NotContain(s => s.Id == sample2.Id);
    }

    [Fact]
    public async Task AuditTrail_NewEntity_CreatesAuditLog()
    {
        // Arrange
        var sample = MockDataFactory.CreateSample(_context.Labs.First().Id);

        // Act
        _context.Samples.Add(sample);
        await _context.SaveChangesAsync();

        // Assert - Query in fresh context to ensure audit logs are visible
        using var verifyContext = _fixture.Container.CreateDbContext();
        var auditLogs = await verifyContext.AuditLogs
            .Where(a => a.EntityId == sample.Id)
            .ToListAsync();
        
        auditLogs.Should().HaveCountGreaterOrEqualTo(1);
        var auditLog = auditLogs.FirstOrDefault(a => a.Action == AuditAction.Create);
        auditLog.Should().NotBeNull();
        auditLog!.EntityType.Should().Be(nameof(Sample));
    }

    [Fact]
    public async Task AuditTrail_UpdatedEntity_CreatesAuditLog()
    {
        // Arrange
        var sample = MockDataFactory.CreateSample(_context.Labs.First().Id);
        _context.Samples.Add(sample);
        await _context.SaveChangesAsync();

        // Clear audit logs from creation
        var createLogs = await _context.AuditLogs.Where(a => a.EntityId == sample.Id).ToListAsync();
        _context.AuditLogs.RemoveRange(createLogs);
        await _context.SaveChangesAsync();

        // Act
        sample.CollectorName = "Updated Collector";
        _context.Samples.Update(sample);
        await _context.SaveChangesAsync();

        // Assert - Query in fresh context to ensure audit logs are visible
        using var verifyContext = _fixture.Container.CreateDbContext();
        var auditLogs = await verifyContext.AuditLogs
            .Where(a => a.EntityId == sample.Id)
            .ToListAsync();
        
        auditLogs.Should().HaveCountGreaterOrEqualTo(1);
        var auditLog = auditLogs.FirstOrDefault(a => a.Action == AuditAction.Update);
        auditLog.Should().NotBeNull();
    }

    [Fact]
    public async Task AuditTrail_DeletedEntity_CreatesAuditLog()
    {
        // Arrange
        var sample = MockDataFactory.CreateSample(_context.Labs.First().Id);
        _context.Samples.Add(sample);
        await _context.SaveChangesAsync();

        // Clear audit logs from creation
        var createLogs = await _context.AuditLogs.Where(a => a.EntityId == sample.Id).ToListAsync();
        _context.AuditLogs.RemoveRange(createLogs);
        await _context.SaveChangesAsync();

        // Act
        _context.Samples.Remove(sample);
        await _context.SaveChangesAsync();

        // Assert - Query in fresh context to ensure audit logs are visible
        // Note: SoftDeleteInterceptor converts Delete to Update (sets IsDeleted=true)
        using var verifyContext = _fixture.Container.CreateDbContext();
        var auditLogs = await verifyContext.AuditLogs
            .Where(a => a.EntityId == sample.Id)
            .ToListAsync();
        
        auditLogs.Should().HaveCountGreaterOrEqualTo(1);
        
        // The audit log should be an Update (soft delete) or Delete (hard delete)
        var auditLog = auditLogs.FirstOrDefault(a => a.Action == AuditAction.Update || a.Action == AuditAction.Delete);
        auditLog.Should().NotBeNull();
    }
}
