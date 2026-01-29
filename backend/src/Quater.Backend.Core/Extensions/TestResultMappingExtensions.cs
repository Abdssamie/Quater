using Quater.Backend.Core.DTOs;
using Quater.Shared.Enums;
using Quater.Shared.Models;

namespace Quater.Backend.Core.Extensions;

/// <summary>
/// Extension methods for mapping between TestResult entity and DTOs
/// </summary>
public static class TestResultMappingExtensions
{
    /// <summary>
    /// Converts TestResult entity to TestResultDto
    /// </summary>
    public static TestResultDto ToDto(this TestResult testResult)
    {
        return new TestResultDto
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

    /// <summary>
    /// Converts CreateTestResultDto to TestResult entity
    /// </summary>
    public static TestResult ToEntity(this CreateTestResultDto dto, string createdBy, ComplianceStatus complianceStatus = ComplianceStatus.Pass)
    {
        var now = DateTime.UtcNow;
        return new TestResult
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
            LastModifiedBy = createdBy,
            IsDeleted = false,
            IsSynced = false,
            CreatedBy = createdBy,
            CreatedDate = now,
            CreatedAt = now,
            LastSyncedAt = DateTime.MinValue
        };
    }

    /// <summary>
    /// Updates TestResult entity from UpdateTestResultDto
    /// </summary>
    public static void UpdateFromDto(this TestResult testResult, UpdateTestResultDto dto, string updatedBy)
    {
        testResult.ParameterName = dto.ParameterName;
        testResult.Value = dto.Value;
        testResult.Unit = dto.Unit;
        testResult.TestDate = dto.TestDate;
        testResult.TechnicianName = dto.TechnicianName;
        testResult.TestMethod = dto.TestMethod;
        testResult.ComplianceStatus = dto.ComplianceStatus;
        testResult.Version = dto.Version;
        testResult.LastModified = DateTime.UtcNow;
        testResult.LastModifiedBy = updatedBy;
        testResult.UpdatedAt = DateTime.UtcNow;
        testResult.UpdatedBy = updatedBy;
        testResult.IsSynced = false;
    }

    /// <summary>
    /// Converts collection of TestResult entities to DTOs
    /// </summary>
    public static IEnumerable<TestResultDto> ToDtos(this IEnumerable<TestResult> testResults)
    {
        return testResults.Select(testResult => testResult.ToDto());
    }
}
