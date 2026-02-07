using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Quater.Backend.Core.Constants;
using Quater.Backend.Core.Tests.Helpers;
using Quater.Backend.Data;
using Quater.Backend.Data.Interceptors;
using Quater.Shared.Enums;
using Quater.Shared.Models;
using System.Text.Json;

namespace Quater.Backend.Core.Tests.Data;

/// <summary>
/// Comprehensive tests for AuditTrailInterceptor using PostgreSQL TestContainers.
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

    #region Basic Audit Operations

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
        auditLog.UserId.Should().Be(SystemUser.GetId()); // Default when no ICurrentUserService
        auditLog.NewValue.Should().NotBeNullOrEmpty();
        auditLog.OldValue.Should().BeNull();
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
        auditLog.OldValue.Should().NotBeNullOrEmpty();
        auditLog.NewValue.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task SaveChanges_DeletedEntity_CreatesAuditLogWithOriginalValue()
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

        // Should have both old and new values (soft delete changes IsDeleted property)
        auditLog.OldValue.Should().NotBeNullOrEmpty();
        auditLog.NewValue.Should().NotBeNullOrEmpty();
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

    #endregion

    #region Property-Level Truncation Tests

    [Fact]
    public async Task SaveChanges_LongStringProperty_TruncatesAt50CharsWithMarker()
    {
        // Arrange
        var labId = await GetFirstLabIdAsync();
        var longString = new string('A', 100); // 100 characters
        var sample = MockDataFactory.CreateSample(labId);
        sample.Notes = longString;

        // Act
        _context.Samples.Add(sample);
        await _context.SaveChangesAsync();

        // Assert
        using var verifyContext = _fixture.Factory.CreateContext();
        var auditLog = await verifyContext.AuditLogs
            .FirstOrDefaultAsync(a => a.EntityId == sample.Id && a.Action == AuditAction.Create);

        auditLog.Should().NotBeNull();
        auditLog!.IsTruncated.Should().BeTrue();
        auditLog.NewValue.Should().NotBeNullOrEmpty();

        // Parse JSON and verify truncation
        var values = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(auditLog.NewValue!);
        values.Should().NotBeNull();
        values!.Should().ContainKey("Notes");

        var notesValue = values?["Notes"].GetString();
        notesValue.Should().EndWith("...[TRUNCATED]");
        notesValue.Should().HaveLength(49); // 35 chars + "...[TRUNCATED]" (14 chars) = 49
    }

    [Fact]
    public async Task SaveChanges_MultiplePropertiesExceed50Chars_TruncatesEachIndividually()
    {
        // Arrange
        var labId = await GetFirstLabIdAsync();
        var longString1 = new string('A', 80);
        var longString2 = new string('B', 90);

        var lab = MockDataFactory.CreateLab();
        lab.ContactInfo = longString1;
        lab.Location = longString2;

        // Act
        _context.Labs.Add(lab);
        await _context.SaveChangesAsync();

        // Assert
        using var verifyContext = _fixture.Factory.CreateContext();
        var auditLog = await verifyContext.AuditLogs
            .FirstOrDefaultAsync(a => a.EntityId == lab.Id && a.Action == AuditAction.Create);

        auditLog.Should().NotBeNull();
        auditLog!.IsTruncated.Should().BeTrue();

        var values = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(auditLog.NewValue!);
        values.Should().NotBeNull();

        // Both properties should be truncated
        var contactInfo = values!["ContactInfo"].GetString();
        contactInfo.Should().EndWith("...[TRUNCATED]");
        contactInfo.Should().HaveLength(49);

        var location = values["Location"].GetString();
        location.Should().EndWith("...[TRUNCATED]");
        location.Should().HaveLength(49);
    }

    [Fact]
    public async Task SaveChanges_ShortStringProperty_DoesNotTruncate()
    {
        // Arrange
        var labId = await GetFirstLabIdAsync();
        var shortString = "Short notes";
        var sample = MockDataFactory.CreateSample(labId);
        sample.Notes = shortString;

        // Act
        _context.Samples.Add(sample);
        await _context.SaveChangesAsync();

        // Assert
        using var verifyContext = _fixture.Factory.CreateContext();
        var auditLog = await verifyContext.AuditLogs
            .FirstOrDefaultAsync(a => a.EntityId == sample.Id && a.Action == AuditAction.Create);

        auditLog.Should().NotBeNull();
        auditLog!.IsTruncated.Should().BeFalse();

        var values = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(auditLog.NewValue!);
        var notesValue = values!["Notes"].GetString();
        notesValue.Should().Be(shortString);
        notesValue.Should().NotContain("TRUNCATED");
    }

    [Fact]
    public async Task SaveChanges_Exactly50CharsProperty_DoesNotTruncate()
    {
        // Arrange
        var labId = await GetFirstLabIdAsync();
        var exactString = new string('A', 50); // Exactly 50 characters
        var sample = MockDataFactory.CreateSample(labId);
        sample.Notes = exactString;

        // Act
        _context.Samples.Add(sample);
        await _context.SaveChangesAsync();

        // Assert
        using var verifyContext = _fixture.Factory.CreateContext();
        var auditLog = await verifyContext.AuditLogs
            .FirstOrDefaultAsync(a => a.EntityId == sample.Id && a.Action == AuditAction.Create);

        auditLog.Should().NotBeNull();
        auditLog!.IsTruncated.Should().BeFalse();

        var values = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(auditLog.NewValue!);
        var notesValue = values!["Notes"].GetString();
        notesValue.Should().Be(exactString);
        notesValue.Should().HaveLength(50);
        notesValue.Should().NotContain("TRUNCATED");
    }

    [Fact]
    public async Task SaveChanges_51CharsProperty_Truncates()
    {
        // Arrange
        var labId = await GetFirstLabIdAsync();
        var string51 = new string('A', 51); // 51 characters (just over limit)
        var sample = MockDataFactory.CreateSample(labId);
        sample.Notes = string51;

        // Act
        _context.Samples.Add(sample);
        await _context.SaveChangesAsync();

        // Assert
        using var verifyContext = _fixture.Factory.CreateContext();
        var auditLog = await verifyContext.AuditLogs
            .FirstOrDefaultAsync(a => a.EntityId == sample.Id && a.Action == AuditAction.Create);

        auditLog.Should().NotBeNull();
        auditLog!.IsTruncated.Should().BeTrue();

        var values = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(auditLog.NewValue!);
        var notesValue = values!["Notes"].GetString();
        notesValue.Should().EndWith("...[TRUNCATED]");
        notesValue.Should().HaveLength(49);
    }

    #endregion

    #region Only Changed Properties Tests

    [Fact]
    public async Task SaveChanges_UpdateSingleProperty_OnlyCapturesThatProperty()
    {
        // Arrange
        var labId = await GetFirstLabIdAsync();
        var sample = MockDataFactory.CreateSample(labId);
        sample.CollectorName = "Original Collector";
        sample.Notes = "Original Notes";

        _context.Samples.Add(sample);
        await _context.SaveChangesAsync();

        // Clear audit logs
        var existingLogs = await _context.AuditLogs.Where(a => a.EntityId == sample.Id).ToListAsync();
        _context.AuditLogs.RemoveRange(existingLogs);
        await _context.SaveChangesAsync();

        // Detach the entity to simulate a fresh load
        _context.Entry(sample).State = EntityState.Detached;

        // Act - Reload entity and only change CollectorName
        var sampleToUpdate = await _context.Samples.FindAsync(sample.Id);
        sampleToUpdate!.CollectorName = "Updated Collector";
        // Don't call Update() - just modify the property and let change tracking detect it
        await _context.SaveChangesAsync();

        // Assert
        using var verifyContext = _fixture.Factory.CreateContext();
        var auditLog = await verifyContext.AuditLogs
            .FirstOrDefaultAsync(a => a.EntityId == sample.Id && a.Action == AuditAction.Update);

        auditLog.Should().NotBeNull();

        var oldValues = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(auditLog!.OldValue!);
        var newValues = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(auditLog.NewValue!);

        // Should contain CollectorName (changed)
        oldValues.Should().ContainKey("CollectorName");
        newValues.Should().ContainKey("CollectorName");

        oldValues?["CollectorName"].GetString().Should().Be("Original Collector");
        newValues?["CollectorName"].GetString().Should().Be("Updated Collector");

        // Should NOT contain Notes (unchanged)
        oldValues.Should().NotContainKey("Notes");
        newValues.Should().NotContainKey("Notes");

        // Note: UpdatedAt/UpdatedBy are only captured if they're actually modified
        // In this test, we're not explicitly setting them, so they may or may not be present
        // depending on whether EF Core's change tracking marks them as modified
    }

    [Fact]
    public async Task SaveChanges_UpdateMultipleProperties_CapturesAllChangedProperties()
    {
        // Arrange
        var labId = await GetFirstLabIdAsync();
        var sample = MockDataFactory.CreateSample(labId);
        sample.CollectorName = "Original Collector";
        sample.Notes = "Original Notes";

        _context.Samples.Add(sample);
        await _context.SaveChangesAsync();

        // Clear audit logs
        var existingLogs = await _context.AuditLogs.Where(a => a.EntityId == sample.Id).ToListAsync();
        _context.AuditLogs.RemoveRange(existingLogs);
        await _context.SaveChangesAsync();

        // Detach the entity to simulate a fresh load
        _context.Entry(sample).State = EntityState.Detached;

        // Act - Reload entity and change both properties
        var sampleToUpdate = await _context.Samples.FindAsync(sample.Id);
        sampleToUpdate!.CollectorName = "Updated Collector";
        sampleToUpdate.Notes = "Updated Notes";
        await _context.SaveChangesAsync();

        // Assert
        using var verifyContext = _fixture.Factory.CreateContext();
        var auditLog = await verifyContext.AuditLogs
            .FirstOrDefaultAsync(a => a.EntityId == sample.Id && a.Action == AuditAction.Update);

        auditLog.Should().NotBeNull();

        var oldValues = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(auditLog!.OldValue!);
        var newValues = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(auditLog.NewValue!);

        // Should contain both changed properties
        oldValues.Should().ContainKey("CollectorName");
        newValues.Should().ContainKey("CollectorName");
        oldValues.Should().ContainKey("Notes");
        newValues.Should().ContainKey("Notes");
    }

    [Fact]
    public async Task SaveChanges_NoPropertiesChanged_DoesNotCreateAuditLog()
    {
        // Arrange
        var labId = await GetFirstLabIdAsync();
        var sample = MockDataFactory.CreateSample(labId);

        _context.Samples.Add(sample);
        await _context.SaveChangesAsync();

        // Clear audit logs
        var existingLogs = await _context.AuditLogs.Where(a => a.EntityId == sample.Id).ToListAsync();
        _context.AuditLogs.RemoveRange(existingLogs);
        await _context.SaveChangesAsync();

        // Detach the entity
        _context.Entry(sample).State = EntityState.Detached;

        // Act - Reload entity but don't change anything
        var sampleToUpdate = await _context.Samples.FindAsync(sample.Id);
        // Don't modify any properties, just call SaveChanges
        await _context.SaveChangesAsync();

        // Assert - No new audit log should be created
        using var verifyContext = _fixture.Factory.CreateContext();
        var auditLogs = await verifyContext.AuditLogs
            .Where(a => a.EntityId == sample.Id)
            .ToListAsync();

        auditLogs.Should().BeEmpty();
    }

    #endregion

    #region System User Fallback Tests

    [Fact]
    public async Task SaveChanges_NoCurrentUserService_UsesSystemAsUserId()
    {
        // Arrange - Default context uses no ICurrentUserService
        var labId = await GetFirstLabIdAsync();
        var sample = MockDataFactory.CreateSample(labId);

        // Act
        _context.Samples.Add(sample);
        await _context.SaveChangesAsync();

        // Assert
        using var verifyContext = _fixture.Factory.CreateContext();
        var auditLog = await verifyContext.AuditLogs
            .FirstOrDefaultAsync(a => a.EntityId == sample.Id);

        auditLog.Should().NotBeNull();
        auditLog!.UserId.Should().Be(SystemUser.GetId());
    }

    [Fact]
    public async Task SaveChanges_WithCurrentUserService_UsesProvidedUserId()
    {
        // Arrange - Create the test user first to satisfy FK constraint
        var labId = await GetFirstLabIdAsync();
        var testUserId = Guid.Parse("550e8400-e29b-41d4-a716-446655440001");
        var testUser = new User
        {
            Id = testUserId,
            UserName = "testuser123",
            NormalizedUserName = "TESTUSER123",
            Email = "testuser123@example.com",
            NormalizedEmail = "TESTUSER123@EXAMPLE.COM",
            EmailConfirmed = true,
            UserLabs = [ new UserLab { LabId = labId, Role = UserRole.Technician } ],
            IsActive = true,
            ConcurrencyStamp = Guid.NewGuid().ToString(),
            SecurityStamp = Guid.NewGuid().ToString()
        };
        _context.Users.Add(testUser);
        await _context.SaveChangesAsync();

        var mockUserService = new MockCurrentUserService(testUserId);
        using var customContext = CreateContextWithUserService(mockUserService);

        var sample = MockDataFactory.CreateSample(labId);

        // Act
        customContext.Samples.Add(sample);
        await customContext.SaveChangesAsync();

        // Assert
        using var verifyContext = _fixture.Factory.CreateContext();
        var auditLog = await verifyContext.AuditLogs
            .FirstOrDefaultAsync(a => a.EntityId == sample.Id);

        auditLog.Should().NotBeNull();
        auditLog!.UserId.Should().Be(testUserId);
    }

    #endregion

    #region EntityType Enum Validation Tests

    [Fact]
    public async Task SaveChanges_AuditableEntityWithValidEntityType_Succeeds()
    {
        // Arrange
        var labId = await GetFirstLabIdAsync();
        var sample = MockDataFactory.CreateSample(labId);

        // Act & Assert - Should not throw
        _context.Samples.Add(sample);
        var act = async () => await _context.SaveChangesAsync();
        await act.Should().NotThrowAsync();

        // Verify audit log was created
        using var verifyContext = _fixture.Factory.CreateContext();
        var auditLog = await verifyContext.AuditLogs
            .FirstOrDefaultAsync(a => a.EntityId == sample.Id);
        auditLog.Should().NotBeNull();
        auditLog!.EntityType.Should().Be(EntityType.Sample);
    }

    [Fact]
    public async Task SaveChanges_LabEntity_CreatesAuditLogWithLabEntityType()
    {
        // Arrange
        var lab = MockDataFactory.CreateLab();

        // Act
        _context.Labs.Add(lab);
        await _context.SaveChangesAsync();

        // Assert
        using var verifyContext = _fixture.Factory.CreateContext();
        var auditLog = await verifyContext.AuditLogs
            .FirstOrDefaultAsync(a => a.EntityId == lab.Id);

        auditLog.Should().NotBeNull();
        auditLog!.EntityType.Should().Be(EntityType.Lab);
    }

    #endregion

    #region Non-Auditable Entity Tests

    [Fact]
    public async Task SaveChanges_UserEntity_DoesNotCreateAuditLog()
    {
        // Arrange - User does NOT implement IAuditable (security reasons)
        var labId = await GetFirstLabIdAsync();
        var userId = Guid.Parse("550e8400-e29b-41d4-a716-446655440002");
        var user = new User
        {
            Id = userId,
            UserName = "testuser",
            NormalizedUserName = "TESTUSER",
            Email = "test@example.com",
            NormalizedEmail = "TEST@EXAMPLE.COM",
            EmailConfirmed = true,
            UserLabs = [ new UserLab { LabId = labId, Role = UserRole.Technician } ],
            IsActive = true,
            ConcurrencyStamp = Guid.NewGuid().ToString(),
            SecurityStamp = Guid.NewGuid().ToString()
        };

        // Act
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Assert - No audit log should be created for User
        using var verifyContext = _fixture.Factory.CreateContext();
        var auditLogs = await verifyContext.AuditLogs
            .Where(a => a.EntityId == userId)
            .ToListAsync();

        auditLogs.Should().BeEmpty();
    }

    #endregion

    #region IP Address Capture Tests

    [Fact]
    public async Task SaveChanges_WithIpAddress_CapturesIpAddress()
    {
        // Arrange
        var ipAddress = "192.168.1.100";
        using var customContext = CreateContextWithIpAddress(ipAddress);

        var labId = await GetFirstLabIdAsync();
        var sample = MockDataFactory.CreateSample(labId);

        // Act
        customContext.Samples.Add(sample);
        await customContext.SaveChangesAsync();

        // Assert
        using var verifyContext = _fixture.Factory.CreateContext();
        var auditLog = await verifyContext.AuditLogs
            .FirstOrDefaultAsync(a => a.EntityId == sample.Id);

        auditLog.Should().NotBeNull();
        auditLog!.IpAddress.Should().Be(ipAddress);
    }

    [Fact]
    public async Task SaveChanges_WithoutIpAddress_IpAddressIsNull()
    {
        // Arrange - Default context has no IP address
        var labId = await GetFirstLabIdAsync();
        var sample = MockDataFactory.CreateSample(labId);

        // Act
        _context.Samples.Add(sample);
        await _context.SaveChangesAsync();

        // Assert
        using var verifyContext = _fixture.Factory.CreateContext();
        var auditLog = await verifyContext.AuditLogs
            .FirstOrDefaultAsync(a => a.EntityId == sample.Id);

        auditLog.Should().NotBeNull();
        auditLog!.IpAddress.Should().BeNull();
    }

    #endregion

    #region Large Entity Handling Tests

    [Fact]
    public async Task SaveChanges_EntityWithManyProperties_JsonStaysUnder4000Chars()
    {
        // Arrange - Create a sample with many long properties
        var labId = await GetFirstLabIdAsync();
        var sample = MockDataFactory.CreateSample(labId);
        sample.Notes = new string('A', 100); // Will be truncated to 50
        sample.CollectorName = new string('B', 100); // Will be truncated to 50

        // Act
        _context.Samples.Add(sample);
        await _context.SaveChangesAsync();

        // Assert
        using var verifyContext = _fixture.Factory.CreateContext();
        var auditLog = await verifyContext.AuditLogs
            .FirstOrDefaultAsync(a => a.EntityId == sample.Id);

        auditLog.Should().NotBeNull();
        auditLog!.NewValue.Should().NotBeNullOrEmpty();

        // Verify JSON is valid and under 4000 chars
        auditLog.NewValue!.Length.Should().BeLessThan(4000);

        // Verify JSON is parseable
        var act = () => JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(auditLog.NewValue!);
        act.Should().NotThrow();
    }

    [Fact]
    public async Task SaveChanges_LabWithAllPropertiesPopulated_JsonIsValid()
    {
        // Arrange - Lab has fewer properties but let's populate them all
        var lab = MockDataFactory.CreateLab("Test Lab with Long Name");
        lab.Location = new string('L', 80); // Will be truncated
        lab.ContactInfo = new string('C', 80); // Will be truncated

        // Act
        _context.Labs.Add(lab);
        await _context.SaveChangesAsync();

        // Assert
        using var verifyContext = _fixture.Factory.CreateContext();
        var auditLog = await verifyContext.AuditLogs
            .FirstOrDefaultAsync(a => a.EntityId == lab.Id);

        auditLog.Should().NotBeNull();
        auditLog!.NewValue.Should().NotBeNullOrEmpty();
        auditLog.NewValue!.Length.Should().BeLessThan(4000);

        // Verify JSON is parseable
        var values = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(auditLog.NewValue!);
        values.Should().NotBeNull();
        values.Should().ContainKey("Name");
        values.Should().ContainKey("Location");
        values.Should().ContainKey("ContactInfo");
    }

    #endregion

    #region Timestamp Tests

    [Fact]
    public async Task SaveChanges_CreatesAuditLog_WithUtcTimestamp()
    {
        // Arrange
        var beforeSave = DateTime.UtcNow;
        var labId = await GetFirstLabIdAsync();
        var sample = MockDataFactory.CreateSample(labId);

        // Act
        _context.Samples.Add(sample);
        await _context.SaveChangesAsync();
        var afterSave = DateTime.UtcNow;

        // Assert
        using var verifyContext = _fixture.Factory.CreateContext();
        var auditLog = await verifyContext.AuditLogs
            .FirstOrDefaultAsync(a => a.EntityId == sample.Id);

        auditLog.Should().NotBeNull();
        auditLog!.Timestamp.Should().BeOnOrAfter(beforeSave);
        auditLog.Timestamp.Should().BeOnOrBefore(afterSave);
        auditLog.Timestamp.Kind.Should().Be(DateTimeKind.Utc);
    }

    #endregion

    #region Helper Methods

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

    private QuaterDbContext CreateContextWithUserService(ICurrentUserService userService)
    {
        var connectionString = $"{_fixture.Factory.ConnectionString};Include Error Detail=true";

        var optionsBuilder = new DbContextOptionsBuilder<QuaterDbContext>()
            .UseNpgsql(connectionString)
            .EnableSensitiveDataLogging()
            .AddInterceptors(
                new SoftDeleteInterceptor(),
                new AuditTrailInterceptor(userService));

        var context = new QuaterDbContext(optionsBuilder.Options);
        context.Database.EnsureCreated();
        return context;
    }

    private QuaterDbContext CreateContextWithIpAddress(string ipAddress)
    {
        var connectionString = $"{_fixture.Factory.ConnectionString};Include Error Detail=true";

        var mockUserService = new MockCurrentUserService(SystemUser.GetId());
        var optionsBuilder = new DbContextOptionsBuilder<QuaterDbContext>()
            .UseNpgsql(connectionString)
            .EnableSensitiveDataLogging()
            .AddInterceptors(
                new SoftDeleteInterceptor(),
                new AuditTrailInterceptor(mockUserService, ipAddress));

        var context = new QuaterDbContext(optionsBuilder.Options);
        context.Database.EnsureCreated();
        return context;
    }

    #endregion
}

/// <summary>
/// Mock implementation of ICurrentUserService for testing.
/// </summary>
public class MockCurrentUserService : ICurrentUserService
{
    private readonly Guid _userId;

    public MockCurrentUserService(Guid userId)
    {
        _userId = userId;
    }

    public Guid GetCurrentUserId() => _userId;
}
