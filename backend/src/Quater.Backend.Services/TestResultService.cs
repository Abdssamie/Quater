using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Quater.Backend.Core.Constants;
using Quater.Backend.Core.DTOs;
using Quater.Backend.Core.Exceptions;
using Quater.Shared.Enums;
using Quater.Backend.Core.Interfaces;
using Quater.Shared.Models;
using Quater.Backend.Data;

namespace Quater.Backend.Services;

public class TestResultService(
    QuaterDbContext context,
    TimeProvider timeProvider,
    IValidator<TestResult> validator) : ITestResultService
{
    public async Task<TestResultDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var testResult = await context.TestResults
            .AsNoTracking()
            .Where(tr => tr.Id == id && !tr.IsDeleted)
            .FirstOrDefaultAsync(ct);

        return testResult == null ? null : MapToDto(testResult);
    }

    public async Task<PagedResult<TestResultDto>> GetAllAsync(int pageNumber = 1, int pageSize = 50, CancellationToken ct = default)
    {
        var query = context.TestResults
            .AsNoTracking()
            .Where(tr => !tr.IsDeleted)
            .OrderByDescending(tr => tr.TestDate);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<TestResultDto>
        {
            Items = items.Select(MapToDto),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<PagedResult<TestResultDto>> GetBySampleIdAsync(Guid sampleId, int pageNumber = 1, int pageSize = 50, CancellationToken ct = default)
    {
        var query = context.TestResults
            .AsNoTracking()
            .Where(tr => tr.SampleId == sampleId && !tr.IsDeleted)
            .OrderByDescending(tr => tr.TestDate);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<TestResultDto>
        {
            Items = items.Select(MapToDto),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<TestResultDto> CreateAsync(CreateTestResultDto dto, string userId, CancellationToken ct = default)
    {
        var now = timeProvider.GetUtcNow().DateTime;

        // Verify sample exists
        var sampleExists = await context.Samples.AnyAsync(s => s.Id == dto.SampleId && !s.IsDeleted, ct);
        if (!sampleExists)
            throw new NotFoundException(ErrorMessages.SampleNotFound);

        // Calculate compliance status based on parameter thresholds
        var complianceStatus = await CalculateComplianceStatusAsync(dto.ParameterName, dto.Value, ct);

        var testResult = new TestResult
        {
            Id = Guid.NewGuid(),
            SampleId = dto.SampleId,
            ParameterName = dto.ParameterName,
            Value = dto.Value,
            Unit = dto.Unit,
            TestDate = dto.TestDate,
            TechnicianName = dto.TechnicianName,
            TestMethod = dto.TestMethod,
            ComplianceStatus = complianceStatus,
            Version = 1,
            LastModified = now,
            LastModifiedBy = userId,
            IsDeleted = false,
            IsSynced = false,
            CreatedBy = userId,
            CreatedDate = now
        };

        // Validate
        await validator.ValidateAndThrowAsync(testResult, ct);

        context.TestResults.Add(testResult);
        await context.SaveChangesAsync(ct);

        return MapToDto(testResult);
    }

    public async Task<TestResultDto?> UpdateAsync(Guid id, UpdateTestResultDto dto, string userId, CancellationToken ct = default)
    {
        var existing = await context.TestResults.FindAsync([id], ct);
        if (existing == null || existing.IsDeleted)
            return null;

        // Check version for optimistic concurrency
        if (existing.Version != dto.Version)
            throw new ConflictException(ErrorMessages.ConcurrencyConflict);

        var now = timeProvider.GetUtcNow().DateTime;

        // Update fields
        existing.ParameterName = dto.ParameterName;
        existing.Value = dto.Value;
        existing.Unit = dto.Unit;
        existing.TestDate = dto.TestDate;
        existing.TechnicianName = dto.TechnicianName;
        existing.TestMethod = dto.TestMethod;
        existing.ComplianceStatus = dto.ComplianceStatus;
        existing.Version += 1;
        existing.LastModified = now;
        existing.LastModifiedBy = userId;
        existing.IsSynced = false;

        // Validate
        await validator.ValidateAndThrowAsync(existing, ct);

        await context.SaveChangesAsync(ct);
        return MapToDto(existing);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var testResult = await context.TestResults.FindAsync([id], ct);
        if (testResult == null || testResult.IsDeleted)
            return false;

        // Soft delete
        testResult.IsDeleted = true;
        testResult.LastModified = timeProvider.GetUtcNow().DateTime;
        testResult.IsSynced = false;

        await context.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>
    /// Calculate compliance status based on WHO/Moroccan standards
    /// </summary>
    private async Task<ComplianceStatus> CalculateComplianceStatusAsync(string parameterName, double value, CancellationToken ct)
    {
        var parameter = await context.Parameters
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Name == parameterName && p.IsActive, ct);

        if (parameter == null)
            return ComplianceStatus.Warning;

        // Check if value is within acceptable range
        if (parameter.MinValue.HasValue && value < parameter.MinValue.Value)
            return ComplianceStatus.Fail;

        if (parameter.MaxValue.HasValue && value > parameter.MaxValue.Value)
            return ComplianceStatus.Fail;

        // Check WHO threshold
        if (parameter.WhoThreshold.HasValue && value > parameter.WhoThreshold.Value)
            return ComplianceStatus.Fail;

        // Check Moroccan threshold
        if (parameter.MoroccanThreshold.HasValue && value > parameter.MoroccanThreshold.Value)
            return ComplianceStatus.Warning;

        return ComplianceStatus.Pass;
    }

    private static TestResultDto MapToDto(TestResult testResult) => new()
    {
        Id = testResult.Id,
        SampleId = testResult.SampleId,
        ParameterName = testResult.ParameterName,
        Value = testResult.Value,
        Unit = testResult.Unit,
        TestDate = testResult.TestDate,
        TechnicianName = testResult.TechnicianName,
        TestMethod = testResult.TestMethod,
        ComplianceStatus = testResult.ComplianceStatus,
        Version = testResult.Version,
        LastModified = testResult.LastModified,
        LastModifiedBy = testResult.LastModifiedBy,
        IsDeleted = testResult.IsDeleted,
        IsSynced = testResult.IsSynced,
        CreatedBy = testResult.CreatedBy,
        CreatedDate = testResult.CreatedDate
    };
}
