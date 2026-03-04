using Microsoft.EntityFrameworkCore;
using Quater.Desktop.Data;
using Quater.Shared.Enums;

namespace Quater.Desktop.Tests.Repositories;

public sealed class SoftDeleteQueryFilterTests : IDisposable
{
    private readonly QuaterLocalContext _context;

    public SoftDeleteQueryFilterTests()
    {
        var options = new DbContextOptionsBuilder<QuaterLocalContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;

        _context = new QuaterLocalContext(options);
        _context.Database.OpenConnection();
        _context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _context.Database.CloseConnection();
        _context.Dispose();
    }

    [Fact]
    public async Task Lab_IsSoftDeleted_FilterExcludesRow()
    {
        var labId = Guid.NewGuid();
        var now = DateTime.UtcNow.ToString("o");
        var createdBy = Guid.NewGuid().ToString("D").ToUpperInvariant();
        var labIdStr = labId.ToString("D").ToUpperInvariant();

#pragma warning disable EF1002
        await _context.Database.ExecuteSqlRawAsync($"""
            INSERT INTO Labs (Id, Name, IsActive, CreatedAt, CreatedBy, IsDeleted, RowVersion, IsSynced)
            VALUES ('{labIdStr}', 'Test Lab', 1, '{now}', '{createdBy}', 1, X'0000000000000001', 0)
            """);
#pragma warning restore EF1002

        var filtered = await _context.Labs.FirstOrDefaultAsync(l => l.Id == labId);
        var ignored = await _context.Labs.IgnoreQueryFilters().FirstOrDefaultAsync(l => l.Id == labId);

        Assert.Null(filtered);
        Assert.NotNull(ignored);
        Assert.True(ignored!.IsDeleted);
    }

    [Fact]
    public async Task Lab_DeletedAtSet_FilterExcludesRow()
    {
        var labId = Guid.NewGuid();
        var now = DateTime.UtcNow.ToString("o");
        var createdBy = Guid.NewGuid().ToString("D").ToUpperInvariant();
        var labIdStr = labId.ToString("D").ToUpperInvariant();

#pragma warning disable EF1002
        await _context.Database.ExecuteSqlRawAsync($"""
            INSERT INTO Labs (Id, Name, IsActive, CreatedAt, CreatedBy, IsDeleted, DeletedAt, RowVersion, IsSynced)
            VALUES ('{labIdStr}', 'DeletedAt Lab', 1, '{now}', '{createdBy}', 0, '{now}', X'0000000000000001', 0)
            """);
#pragma warning restore EF1002

        var filtered = await _context.Labs.FirstOrDefaultAsync(l => l.Id == labId);
        var ignored = await _context.Labs.IgnoreQueryFilters().FirstOrDefaultAsync(l => l.Id == labId);

        Assert.Null(filtered);
        Assert.NotNull(ignored);
        Assert.False(ignored!.IsDeleted);
        Assert.NotNull(ignored.DeletedAt);
    }

    [Fact]
    public async Task Parameter_IsSoftDeleted_FilterExcludesRow()
    {
        var parameterId = Guid.NewGuid();
        var now = DateTime.UtcNow.ToString("o");
        var createdBy = Guid.NewGuid().ToString("D").ToUpperInvariant();
        var parameterIdStr = parameterId.ToString("D").ToUpperInvariant();

#pragma warning disable EF1002
        await _context.Database.ExecuteSqlRawAsync($"""
            INSERT INTO Parameters (
                Id, Name, Unit, Threshold, MinValue, MaxValue, Description,
                IsActive, CreatedAt, CreatedBy, IsDeleted, RowVersion, IsSynced)
            VALUES (
                '{parameterIdStr}', 'pH', 'pH', 7.0, 0.0, 14.0, 'pH Level',
                1, '{now}', '{createdBy}', 1, X'0000000000000001', 0)
            """);
#pragma warning restore EF1002

        var filtered = await _context.Parameters.FirstOrDefaultAsync(p => p.Id == parameterId);
        var ignored = await _context.Parameters.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == parameterId);

        Assert.Null(filtered);
        Assert.NotNull(ignored);
        Assert.True(ignored!.IsDeleted);
    }

    [Fact]
    public async Task Parameter_DeletedAtSet_FilterExcludesRow()
    {
        var parameterId = Guid.NewGuid();
        var now = DateTime.UtcNow.ToString("o");
        var createdBy = Guid.NewGuid().ToString("D").ToUpperInvariant();
        var parameterIdStr = parameterId.ToString("D").ToUpperInvariant();

#pragma warning disable EF1002
        await _context.Database.ExecuteSqlRawAsync($"""
            INSERT INTO Parameters (
                Id, Name, Unit, Threshold, MinValue, MaxValue, Description,
                IsActive, CreatedAt, CreatedBy, IsDeleted, DeletedAt, RowVersion, IsSynced)
            VALUES (
                '{parameterIdStr}', 'DeletedAt Param', 'mg/L', 2.0, 0.0, 4.0, 'DeletedAt',
                1, '{now}', '{createdBy}', 0, '{now}', X'0000000000000001', 0)
            """);
#pragma warning restore EF1002

        var filtered = await _context.Parameters.FirstOrDefaultAsync(p => p.Id == parameterId);
        var ignored = await _context.Parameters.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == parameterId);

        Assert.Null(filtered);
        Assert.NotNull(ignored);
        Assert.False(ignored!.IsDeleted);
        Assert.NotNull(ignored.DeletedAt);
    }

    [Fact]
    public async Task Sample_DeletedAtSet_FilterExcludesRow()
    {
        var labId = Guid.NewGuid();
        var sampleId = Guid.NewGuid();
        var now = DateTime.UtcNow.ToString("o");
        var createdBy = Guid.NewGuid().ToString("D").ToUpperInvariant();

        var labIdStr = labId.ToString("D").ToUpperInvariant();
        var sampleIdStr = sampleId.ToString("D").ToUpperInvariant();

#pragma warning disable EF1002
        await _context.Database.ExecuteSqlRawAsync($"""
            INSERT INTO Labs (Id, Name, IsActive, CreatedAt, CreatedBy, IsDeleted, RowVersion, IsSynced)
            VALUES ('{labIdStr}', 'Test Lab', 1, '{now}', '{createdBy}', 0, X'0000000000000001', 0)
            """);

        await _context.Database.ExecuteSqlRawAsync($"""
            INSERT INTO Samples (
                Id, Type, LocationLatitude, LocationLongitude, LocationDescription,
                CollectionDate, CollectorName, Status, LabId,
                CreatedAt, CreatedBy, IsDeleted, DeletedAt, RowVersion, IsSynced)
            VALUES (
                '{sampleIdStr}', 'DrinkingWater', 34.0, -6.8, 'DeletedAt Site',
                '{now}', 'Test Collector', 'Pending', '{labIdStr}',
                '{now}', '{createdBy}', 0, '{now}', X'0000000000000001', 0)
            """);
#pragma warning restore EF1002

        var filtered = await _context.Samples.FirstOrDefaultAsync(s => s.Id == sampleId);
        var ignored = await _context.Samples.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.Id == sampleId);

        Assert.Null(filtered);
        Assert.NotNull(ignored);
        Assert.False(ignored!.IsDeleted);
        Assert.NotNull(ignored.DeletedAt);
    }

    [Fact]
    public async Task TestResult_IsSoftDeleted_FilterExcludesRow()
    {
        var labId = Guid.NewGuid();
        var sampleId = Guid.NewGuid();
        var parameterId = Guid.NewGuid();
        var testResultId = Guid.NewGuid();
        var now = DateTime.UtcNow.ToString("o");
        var createdBy = Guid.NewGuid().ToString("D").ToUpperInvariant();

        var labIdStr = labId.ToString("D").ToUpperInvariant();
        var sampleIdStr = sampleId.ToString("D").ToUpperInvariant();
        var parameterIdStr = parameterId.ToString("D").ToUpperInvariant();
        var testResultIdStr = testResultId.ToString("D").ToUpperInvariant();

#pragma warning disable EF1002
        await _context.Database.ExecuteSqlRawAsync($"""
            INSERT INTO Labs (Id, Name, IsActive, CreatedAt, CreatedBy, IsDeleted, RowVersion, IsSynced)
            VALUES ('{labIdStr}', 'Test Lab', 1, '{now}', '{createdBy}', 0, X'0000000000000001', 0)
            """);

        await _context.Database.ExecuteSqlRawAsync($"""
            INSERT INTO Samples (
                Id, Type, LocationLatitude, LocationLongitude, LocationDescription,
                CollectionDate, CollectorName, Status, LabId,
                CreatedAt, CreatedBy, IsDeleted, RowVersion, IsSynced)
            VALUES (
                '{sampleIdStr}', 'DrinkingWater', 34.0, -6.8, 'Test Site',
                '{now}', 'Test Collector', 'Pending', '{labIdStr}',
                '{now}', '{createdBy}', 0, X'0000000000000001', 0)
            """);

        await _context.Database.ExecuteSqlRawAsync($"""
            INSERT INTO Parameters (
                Id, Name, Unit, Threshold, MinValue, MaxValue, Description,
                IsActive, CreatedAt, CreatedBy, IsDeleted, RowVersion, IsSynced)
            VALUES (
                '{parameterIdStr}', 'Chlorine', 'mg/L', 2.0, 0.0, 4.0, 'Chlorine',
                1, '{now}', '{createdBy}', 0, X'0000000000000001', 0)
            """);

        await _context.Database.ExecuteSqlRawAsync($"""
            INSERT INTO TestResults (
                Id, SampleId, ParameterId, Value, Unit, Status, TestDate, TechnicianName,
                TestMethod, ComplianceStatus, IsVoided, IsDeleted, CreatedAt, CreatedBy,
                RowVersion, IsSynced)
            VALUES (
                '{testResultIdStr}', '{sampleIdStr}', '{parameterIdStr}', 1.23, 'mg/L', 'Draft', '{now}',
                'Tech', 'Titration', 'Pass', 0, 1, '{now}', '{createdBy}', X'0000000000000001', 0)
            """);
#pragma warning restore EF1002

        var filtered = await _context.TestResults.FirstOrDefaultAsync(r => r.Id == testResultId);
        var ignored = await _context.TestResults.IgnoreQueryFilters().FirstOrDefaultAsync(r => r.Id == testResultId);

        Assert.Null(filtered);
        Assert.NotNull(ignored);
        Assert.True(ignored!.IsDeleted);
    }

    [Fact]
    public async Task TestResult_DeletedAtSet_FilterExcludesRow()
    {
        var labId = Guid.NewGuid();
        var sampleId = Guid.NewGuid();
        var parameterId = Guid.NewGuid();
        var testResultId = Guid.NewGuid();
        var now = DateTime.UtcNow.ToString("o");
        var createdBy = Guid.NewGuid().ToString("D").ToUpperInvariant();

        var labIdStr = labId.ToString("D").ToUpperInvariant();
        var sampleIdStr = sampleId.ToString("D").ToUpperInvariant();
        var parameterIdStr = parameterId.ToString("D").ToUpperInvariant();
        var testResultIdStr = testResultId.ToString("D").ToUpperInvariant();

#pragma warning disable EF1002
        await _context.Database.ExecuteSqlRawAsync($"""
            INSERT INTO Labs (Id, Name, IsActive, CreatedAt, CreatedBy, IsDeleted, RowVersion, IsSynced)
            VALUES ('{labIdStr}', 'Test Lab', 1, '{now}', '{createdBy}', 0, X'0000000000000001', 0)
            """);

        await _context.Database.ExecuteSqlRawAsync($"""
            INSERT INTO Samples (
                Id, Type, LocationLatitude, LocationLongitude, LocationDescription,
                CollectionDate, CollectorName, Status, LabId,
                CreatedAt, CreatedBy, IsDeleted, RowVersion, IsSynced)
            VALUES (
                '{sampleIdStr}', 'DrinkingWater', 34.0, -6.8, 'Test Site',
                '{now}', 'Test Collector', 'Pending', '{labIdStr}',
                '{now}', '{createdBy}', 0, X'0000000000000001', 0)
            """);

        await _context.Database.ExecuteSqlRawAsync($"""
            INSERT INTO Parameters (
                Id, Name, Unit, Threshold, MinValue, MaxValue, Description,
                IsActive, CreatedAt, CreatedBy, IsDeleted, RowVersion, IsSynced)
            VALUES (
                '{parameterIdStr}', 'Chlorine', 'mg/L', 2.0, 0.0, 4.0, 'Chlorine',
                1, '{now}', '{createdBy}', 0, X'0000000000000001', 0)
            """);

        await _context.Database.ExecuteSqlRawAsync($"""
            INSERT INTO TestResults (
                Id, SampleId, ParameterId, Value, Unit, Status, TestDate, TechnicianName,
                TestMethod, ComplianceStatus, IsVoided, IsDeleted, DeletedAt, CreatedAt, CreatedBy,
                RowVersion, IsSynced)
            VALUES (
                '{testResultIdStr}', '{sampleIdStr}', '{parameterIdStr}', 1.23, 'mg/L', 'Draft', '{now}',
                'Tech', 'Titration', 'Pass', 0, 0, '{now}', '{now}', '{createdBy}', X'0000000000000001', 0)
            """);
#pragma warning restore EF1002

        var filtered = await _context.TestResults.FirstOrDefaultAsync(r => r.Id == testResultId);
        var ignored = await _context.TestResults.IgnoreQueryFilters().FirstOrDefaultAsync(r => r.Id == testResultId);

        Assert.Null(filtered);
        Assert.NotNull(ignored);
        Assert.False(ignored!.IsDeleted);
        Assert.NotNull(ignored.DeletedAt);
    }

    [Fact]
    public async Task TestResult_SampleDeletedAtSet_FilterExcludesRow()
    {
        var labId = Guid.NewGuid();
        var sampleId = Guid.NewGuid();
        var parameterId = Guid.NewGuid();
        var testResultId = Guid.NewGuid();
        var now = DateTime.UtcNow.ToString("o");
        var createdBy = Guid.NewGuid().ToString("D").ToUpperInvariant();

        var labIdStr = labId.ToString("D").ToUpperInvariant();
        var sampleIdStr = sampleId.ToString("D").ToUpperInvariant();
        var parameterIdStr = parameterId.ToString("D").ToUpperInvariant();
        var testResultIdStr = testResultId.ToString("D").ToUpperInvariant();

#pragma warning disable EF1002
        await _context.Database.ExecuteSqlRawAsync($"""
            INSERT INTO Labs (Id, Name, IsActive, CreatedAt, CreatedBy, IsDeleted, RowVersion, IsSynced)
            VALUES ('{labIdStr}', 'Test Lab', 1, '{now}', '{createdBy}', 0, X'0000000000000001', 0)
            """);

        await _context.Database.ExecuteSqlRawAsync($"""
            INSERT INTO Samples (
                Id, Type, LocationLatitude, LocationLongitude, LocationDescription,
                CollectionDate, CollectorName, Status, LabId,
                CreatedAt, CreatedBy, IsDeleted, DeletedAt, RowVersion, IsSynced)
            VALUES (
                '{sampleIdStr}', 'DrinkingWater', 34.0, -6.8, 'DeletedAt Site',
                '{now}', 'Test Collector', 'Pending', '{labIdStr}',
                '{now}', '{createdBy}', 0, '{now}', X'0000000000000001', 0)
            """);

        await _context.Database.ExecuteSqlRawAsync($"""
            INSERT INTO Parameters (
                Id, Name, Unit, Threshold, MinValue, MaxValue, Description,
                IsActive, CreatedAt, CreatedBy, IsDeleted, RowVersion, IsSynced)
            VALUES (
                '{parameterIdStr}', 'Chlorine', 'mg/L', 2.0, 0.0, 4.0, 'Chlorine',
                1, '{now}', '{createdBy}', 0, X'0000000000000001', 0)
            """);

        await _context.Database.ExecuteSqlRawAsync($"""
            INSERT INTO TestResults (
                Id, SampleId, ParameterId, Value, Unit, Status, TestDate, TechnicianName,
                TestMethod, ComplianceStatus, IsVoided, IsDeleted, CreatedAt, CreatedBy,
                RowVersion, IsSynced)
            VALUES (
                '{testResultIdStr}', '{sampleIdStr}', '{parameterIdStr}', 1.23, 'mg/L', 'Draft', '{now}',
                'Tech', 'Titration', 'Pass', 0, 0, '{now}', '{createdBy}', X'0000000000000001', 0)
            """);
#pragma warning restore EF1002

        var filtered = await _context.TestResults.FirstOrDefaultAsync(r => r.Id == testResultId);
        var ignored = await _context.TestResults.IgnoreQueryFilters().FirstOrDefaultAsync(r => r.Id == testResultId);

        Assert.Null(filtered);
        Assert.NotNull(ignored);
        Assert.False(ignored!.IsDeleted);
    }
}
