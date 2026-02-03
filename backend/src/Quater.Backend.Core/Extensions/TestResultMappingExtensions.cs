using Quater.Backend.Core.DTOs;
using Quater.Shared.Enums;
using Quater.Shared.Models;
using Quater.Shared.ValueObjects;

namespace Quater.Backend.Core.Extensions;

/// <summary>
/// Extension methods for mapping between TestResult entity and DTOs
/// </summary>
public static class TestResultMappingExtensions
{
    /// <summary>
    /// Converts TestResult entity to TestResultDto
    /// </summary>
    /// <param name="testResult">The TestResult entity</param>
    /// <param name="parameterName">The parameter name (must be provided since model uses ParameterId)</param>
    public static TestResultDto ToDto(this TestResult testResult, string parameterName)
    {
        return new TestResultDto
        {
            Id = testResult.Id,
            SampleId = testResult.SampleId,
            ParameterName = parameterName,
            Value = testResult.Measurement.Value,
            Unit = testResult.Measurement.Unit,
            TestDate = testResult.TestDate,
            TechnicianName = testResult.TechnicianName,
            TestMethod = testResult.TestMethod,
            ComplianceStatus = testResult.ComplianceStatus,
            Version = 1, // Version removed from model, using constant for backward compatibility
            LastModified = testResult.UpdatedAt ?? testResult.CreatedAt,
            LastModifiedBy = testResult.UpdatedBy ?? testResult.CreatedBy,
            IsDeleted = testResult.IsDeleted,
            CreatedBy = testResult.CreatedBy,
            CreatedDate = testResult.CreatedAt
        };
    }

    /// <summary>
    /// Converts CreateTestResultDto to TestResult entity
    /// </summary>
    /// <param name="dto">The DTO containing test result data</param>
    /// <param name="parameter">The Parameter entity (required for Measurement ValueObject creation)</param>
    /// <param name="createdBy">Username of the creator</param>
    /// <param name="complianceStatus">Compliance status (defaults to Pass)</param>
    public static TestResult ToEntity(this CreateTestResultDto dto, Parameter parameter, string createdBy, ComplianceStatus complianceStatus = ComplianceStatus.Pass)
    {
        var now = DateTime.UtcNow;
        return new TestResult
        {
            Id = Guid.NewGuid(),
            SampleId = dto.SampleId,
            Measurement = new Measurement(parameter, dto.Value, dto.Unit),
            TestDate = dto.TestDate,
            TechnicianName = dto.TechnicianName,
            TestMethod = dto.TestMethod,
            ComplianceStatus = complianceStatus,
            Status = TestResultStatus.Draft,
            CreatedBy = createdBy,
            CreatedAt = now,
            LastSyncedAt = DateTime.MinValue
        };
    }

    /// <summary>
    /// Updates TestResult entity from UpdateTestResultDto
    /// </summary>
    /// <param name="testResult">The TestResult entity to update</param>
    /// <param name="dto">The DTO containing updated data</param>
    /// <param name="parameter">The Parameter entity (required for Measurement ValueObject creation)</param>
    /// <param name="updatedBy">Username of the updater</param>
    public static void UpdateFromDto(this TestResult testResult, UpdateTestResultDto dto, Parameter parameter, string updatedBy)
    {
        testResult.Measurement = new Measurement(parameter, dto.Value, dto.Unit);
        testResult.TestDate = dto.TestDate;
        testResult.TechnicianName = dto.TechnicianName;
        testResult.TestMethod = dto.TestMethod;
        testResult.ComplianceStatus = dto.ComplianceStatus;
        testResult.UpdatedAt = DateTime.UtcNow;
        testResult.UpdatedBy = updatedBy;
    }

    /// <summary>
    /// Converts collection of TestResult entities to DTOs
    /// </summary>
    /// <param name="testResults">Collection of TestResult entities</param>
    /// <param name="parameterLookup">Dictionary mapping ParameterId to ParameterName</param>
    public static IEnumerable<TestResultDto> ToDtos(this IEnumerable<TestResult> testResults, Dictionary<Guid, string> parameterLookup)
    {
        return testResults.Select(testResult => 
        {
            var parameterName = parameterLookup.TryGetValue(testResult.Measurement.ParameterId, out var name) 
                ? name 
                : "Unknown";
            return testResult.ToDto(parameterName);
        });
    }
}
