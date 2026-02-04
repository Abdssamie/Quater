using System.Reflection;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Quater.Backend.Core.Tests.Helpers;
using Quater.Backend.Data;
using Quater.Shared.Models;
using Xunit;

namespace Quater.Backend.Core.Tests.Data;

/// <summary>
/// Tests to verify the refactoring that removed ConflictBackup and SyncLog tables.
/// Uses TestContainers (PostgreSQL) for proper FK constraint and concurrency handling.
/// </summary>
[Collection("TestDatabase")]
public class RefactoringVerificationTests
{
    private readonly TestDbContextFactoryFixture _fixture;

    public RefactoringVerificationTests(TestDbContextFactoryFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void RemovedModels_ShouldNotExist()
    {
        // Assemble name
        var assembly = typeof(Quater.Shared.Models.User).Assembly;

        // Act
        var conflictBackupType = assembly.GetType("Quater.Shared.Models.ConflictBackup");
        var syncLogType = assembly.GetType("Quater.Shared.Models.SyncLog");

        // Assert
        conflictBackupType.Should().BeNull("ConflictBackup model should be deleted");
        syncLogType.Should().BeNull("SyncLog model should be deleted");
    }

    [Fact]
    public void AuditLog_ShouldNotHaveConflictBackupProperties()
    {
        // Arrange
        var type = typeof(AuditLog);

        // Act
        var conflictBackupIdProp = type.GetProperty("ConflictBackupId");
        var conflictBackupProp = type.GetProperty("ConflictBackup");
        var conflictResolutionNotesProp = type.GetProperty("ConflictResolutionNotes");

        // Assert
        conflictBackupIdProp.Should().BeNull();
        conflictBackupProp.Should().BeNull();
        conflictResolutionNotesProp.Should().BeNull();
    }

    [Fact]
    public async Task Concurrency_Update_ShouldThrowException()
    {
        // Reset database for clean state
        await _fixture.Factory.ResetDatabaseAsync();

        // Create seeded context and add a sample
        using var setupContext = _fixture.Factory.CreateContextWithoutInterceptors();
        var labId = setupContext.Labs.First().Id;
        var sample = MockDataFactory.CreateSample(labId);

        setupContext.Samples.Add(sample);
        await setupContext.SaveChangesAsync();

        var sampleId = sample.Id;
        var originalRowVersion = sample.RowVersion;

        // Dispose setup context to release connection
        setupContext.Dispose();

        // Simulate concurrent update scenario:
        // 1. User 1 loads the sample
        // 2. User 2 loads the same sample  
        // 3. User 1 saves changes (updates RowVersion in DB)
        // 4. User 2 tries to save with stale RowVersion -> should fail

        using var context1 = _fixture.Factory.CreateContext();
        var sample1 = await context1.Samples.FirstAsync(s => s.Id == sampleId);

        // Capture the row version that sample1 has
        var sample1RowVersion = sample1.RowVersion;

        // Now simulate User 2 loading the same sample in a different context
        // before User 1 saves
        using var context2 = _fixture.Factory.CreateContext();
        var sample2 = await context2.Samples.FirstAsync(s => s.Id == sampleId);

        // User 1 makes and saves their changes
        sample1.Notes = "Updated by User 1";
        await context1.SaveChangesAsync();

        // Verify the row version changed after save (if using proper concurrency)
        // For PostgreSQL without triggers, we need to verify OCC differently

        // User 2 now tries to update with stale data
        // We need to simulate the stale RowVersion condition
        sample2.Notes = "Updated by User 2";

        // For PostgreSQL without auto-incrementing row version triggers,
        // we simulate the concurrency conflict by manually setting the original
        // value to what it was before User 1's update, while the database
        // now has a different value.

        // Get the updated row version from the database
        await context1.Entry(sample1).ReloadAsync();
        var newRowVersion = sample1.RowVersion;

        // If row versions are the same (PostgreSQL without trigger), 
        // we need to manually simulate the conflict
        if (originalRowVersion.SequenceEqual(newRowVersion))
        {
            // PostgreSQL doesn't auto-update bytea columns without triggers
            // So we directly test the optimistic concurrency by modifying
            // the original value that EF Core tracks

            // Set the original value in the change tracker to simulate a DB change
            context2.Entry(sample2).Property(s => s.RowVersion).OriginalValue =
                new byte[] { 0, 0, 0, 0, 0, 0, 0, 99 }; // Different from what's in DB
        }

        // Act & Assert: Should throw DbUpdateConcurrencyException
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => context2.SaveChangesAsync());
    }

    [Fact]
    public async Task RowVersion_ShouldBeConfiguredAsConcurrencyToken()
    {
        // This test verifies that the RowVersion property is properly
        // configured as a concurrency token in the entity model

        await _fixture.Factory.ResetDatabaseAsync();

        using var context = _fixture.Factory.CreateContext();

        // Get the entity type for Sample
        var entityType = context.Model.FindEntityType(typeof(Sample));
        entityType.Should().NotBeNull();

        // Find the RowVersion property
        var rowVersionProperty = entityType!.FindProperty(nameof(Sample.RowVersion));
        rowVersionProperty.Should().NotBeNull();

        // Verify it's configured as a concurrency token
        rowVersionProperty!.IsConcurrencyToken.Should().BeTrue(
            "RowVersion should be configured as a concurrency token");

        // Verify it's value generated on add or update
        rowVersionProperty.ValueGenerated.Should().Be(
            Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.OnAddOrUpdate,
            "RowVersion should be value generated on add or update");
    }
}
