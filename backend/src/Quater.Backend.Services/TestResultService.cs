using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Quater.Backend.Core.Constants;
using Quater.Backend.Core.DTOs;
using Quater.Backend.Core.Exceptions;
using Quater.Backend.Core.Extensions;
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

        if (testResult == null)
            return null;

        // Look up parameter name
        var parameter = await context.Parameters
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == testResult.Measurement.ParameterId, ct);

        var parameterName = parameter?.Name ?? "Unknown";
        return testResult.ToDto(parameterName);
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

        // Build parameter lookup dictionary
        var parameterIds = items.Select(tr => tr.Measurement.ParameterId).Distinct().ToList();
        var parameters = await context.Parameters
            .AsNoTracking()
            .Where(p => parameterIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.Name, ct);

        return new PagedResult<TestResultDto>
        {
            Items = items.ToDtos(parameters),
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

        // Build parameter lookup dictionary
        var parameterIds = items.Select(tr => tr.Measurement.ParameterId).Distinct().ToList();
        var parameters = await context.Parameters
            .AsNoTracking()
            .Where(p => parameterIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.Name, ct);

        return new PagedResult<TestResultDto>
        {
            Items = items.ToDtos(parameters),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<TestResultDto> CreateAsync(CreateTestResultDto dto, string userId, CancellationToken ct = default)
    {
        // Verify sample exists
        var sampleExists = await context.Samples.AnyAsync(s => s.Id == dto.SampleId && !s.IsDeleted, ct);
        if (!sampleExists)
            throw new NotFoundException(ErrorMessages.SampleNotFound);

        // Look up parameter by name to get the Parameter entity
        var parameter = await context.Parameters
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Name == dto.ParameterName && p.IsActive, ct);
        
        if (parameter == null)
            throw new NotFoundException($"Parameter '{dto.ParameterName}' not found");

        // Calculate compliance status based on parameter thresholds
        var complianceStatus = await CalculateComplianceStatusAsync(dto.ParameterName, dto.Value, ct);

        // Use extension method to create entity with Measurement ValueObject
        var testResult = dto.ToEntity(parameter, userId, complianceStatus);

        // Validate
        await validator.ValidateAndThrowAsync(testResult, ct);

        context.TestResults.Add(testResult);
        await context.SaveChangesAsync(ct);

        return testResult.ToDto(parameter.Name);
    }

    public async Task<TestResultDto?> UpdateAsync(Guid id, UpdateTestResultDto dto, string userId, CancellationToken ct = default)
    {
        var existing = await context.TestResults.FindAsync([id], ct);
        if (existing == null || existing.IsDeleted)
            return null;

        // Look up parameter by name to get the Parameter entity
        var parameter = await context.Parameters
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Name == dto.ParameterName && p.IsActive, ct);
        
        if (parameter == null)
            throw new NotFoundException($"Parameter '{dto.ParameterName}' not found");

        // Use extension method to update entity with Measurement ValueObject
        existing.UpdateFromDto(dto, parameter, userId);

        // Validate
        await validator.ValidateAndThrowAsync(existing, ct);

        try
        {
            await context.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException(ErrorMessages.ConcurrencyConflict);
        }

        return existing.ToDto(parameter.Name);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var testResult = await context.TestResults.FindAsync([id], ct);
        if (testResult == null || testResult.IsDeleted)
            return false;

        // Soft delete
        testResult.IsDeleted = true;
        testResult.UpdatedAt = timeProvider.GetUtcNow().DateTime;
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
}
