using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Quater.Backend.Core.Tests.Helpers;
using Quater.Backend.Data;
using Quater.Shared.Enums;
using Quater.Shared.Models;
using Xunit;

namespace Quater.Backend.Core.Tests.Data;

[Collection("TestDatabase")]
public class AuditLogArchiveMigrationTests
{
    private const string PreConversionMigration = "20260303102636_FixTestResultsRlsPolicy";
    private const string ConversionMigration = "20260304082309_FixAuditLogArchiveEnumConversion";

    private readonly TestDbContextFactoryFixture _fixture;

    public AuditLogArchiveMigrationTests(TestDbContextFactoryFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Migrate_ConvertsAuditLogArchiveEnumValuesToNames()
    {
        var options = new DbContextOptionsBuilder<QuaterDbContext>()
            .UseNpgsql(_fixture.Factory.ConnectionString)
            .EnableSensitiveDataLogging()
            .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning))
            .Options;

        await using var context = new QuaterDbContext(options);
        await context.Database.EnsureDeletedAsync();

        var migrator = context.Database.GetService<IMigrator>();
        await migrator.MigrateAsync(PreConversionMigration);

        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = "archive-test",
            NormalizedUserName = "ARCHIVE-TEST",
            Email = "archive-test@quater.app",
            NormalizedEmail = "ARCHIVE-TEST@QUATER.APP",
            EmailConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
            IsActive = true,
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        var archiveId = Guid.NewGuid();
        var entityId = Guid.NewGuid();
        var timestamp = DateTime.UtcNow;

        await context.Database.ExecuteSqlRawAsync(@"
INSERT INTO ""AuditLogArchive"" (
    ""Id"", ""UserId"", ""EntityType"", ""EntityId"", ""Action"",
    ""OldValue"", ""NewValue"", ""IsTruncated"", ""Timestamp"", ""IpAddress"", ""ArchivedDate""
) VALUES ({0}, {1}, {2}, {3}, {4}, NULL, NULL, {5}, {6}, NULL, {7});
",
            archiveId,
            user.Id,
            2,
            entityId,
            2,
            false,
            timestamp,
            timestamp);

        await migrator.MigrateAsync(ConversionMigration);

        var convertedAction = await context.Database
            .SqlQueryRaw<string>("SELECT \"Action\" AS \"Value\" FROM \"AuditLogArchive\" WHERE \"Id\" = {0}",
                archiveId)
            .SingleAsync();

        var convertedEntityType = await context.Database
            .SqlQueryRaw<string>("SELECT \"EntityType\" AS \"Value\" FROM \"AuditLogArchive\" WHERE \"Id\" = {0}",
                archiveId)
            .SingleAsync();

        convertedAction.Should().Be(nameof(AuditAction.Delete));
        convertedEntityType.Should().Be(nameof(EntityType.Sample));
    }
}
