using Quater.Shared.Enums;
using Quater.Shared.Models;

namespace Quater.Backend.Core.Tests.Helpers;

/// <summary>
/// Factory for creating mock test data
/// </summary>
public static class MockDataFactory
{
    private static readonly DateTime BaseDate = new(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// Creates a complete test data set with related entities
    /// </summary>
    public static TestDataSet CreateTestDataSet()
    {
        var labs = CreateLabs();
        var parameters = CreateParameters();
        var samples = CreateSamples(labs);
        var testResults = CreateTestResults(samples, parameters);

        return new TestDataSet
        {
            Labs = labs,
            Parameters = parameters,
            Samples = samples,
            TestResults = testResults
        };
    }

    /// <summary>
    /// Creates a test lab
    /// </summary>
    public static Lab CreateLab(string name = "Test Lab", string? location = null)
    {
        return new Lab
        {
            Id = Guid.NewGuid(),
            Name = name,
            Location = location ?? "Test Location",
            ContactInfo = $"{name.Replace(" ", "").ToLower()}@test.com, +212-123-456-789",
            IsActive = true,
            CreatedDate = BaseDate,
            CreatedBy = "test",
            CreatedAt = BaseDate,
            IsDeleted = false
        };
    }

    /// <summary>
    /// Creates multiple test labs
    /// </summary>
    public static List<Lab> CreateLabs(int count = 3)
    {
        var labs = new List<Lab>();
        for (int i = 1; i <= count; i++)
        {
            labs.Add(CreateLab($"Lab {i}", $"Location {i}"));
        }
        return labs;
    }

    /// <summary>
    /// Creates a test parameter
    /// </summary>
    public static Parameter CreateParameter(
        string name = "pH",
        string unit = "pH units",
        double? whoThreshold = 8.5,
        double? moroccanThreshold = 9.0,
        double? minValue = 6.5,
        double? maxValue = 9.5)
    {
        return new Parameter
        {
            Id = Guid.NewGuid(),
            Name = name,
            Unit = unit,
            WhoThreshold = whoThreshold,
            MoroccanThreshold = moroccanThreshold,
            MinValue = minValue,
            MaxValue = maxValue,
            IsActive = true,
            CreatedDate = BaseDate,
            LastModified = BaseDate,
            CreatedBy = "test",
            CreatedAt = BaseDate,
            IsDeleted = false
        };
    }

    /// <summary>
    /// Creates multiple test parameters
    /// </summary>
    public static List<Parameter> CreateParameters()
    {
        return new List<Parameter>
        {
            CreateParameter("pH", "pH units", 8.5, 9.0, 6.5, 9.5),
            CreateParameter("Turbidity", "NTU", 5.0, 10.0, 0, null),
            CreateParameter("Chlorine", "mg/L", 5.0, null, 0.2, 5.0),
            CreateParameter("Temperature", "°C", 25.0, 30.0, 0, 40.0),
            CreateParameter("Conductivity", "µS/cm", 2500, 2700, null, null)
        };
    }

    /// <summary>
    /// Creates a test sample
    /// </summary>
    public static Sample CreateSample(
        Guid? labId = null,
        SampleType type = SampleType.DrinkingWater,
        SampleStatus status = SampleStatus.Pending,
        string collectorName = "John Doe")
    {
        return new Sample
        {
            Id = Guid.NewGuid(),
            Type = type,
            LocationLatitude = 34.0,
            LocationLongitude = -5.0,
            LocationDescription = "Test Location",
            LocationHierarchy = "Country/Region/City",
            CollectionDate = BaseDate,
            CollectorName = collectorName,
            Notes = "Test sample notes",
            Status = status,
            LabId = labId ?? Guid.NewGuid(),
            CreatedDate = BaseDate,
            LastModified = BaseDate,
            CreatedBy = "test",
            LastModifiedBy = "test",
            CreatedAt = BaseDate,
            Version = 1,
            IsDeleted = false,
            IsSynced = true
        };
    }

    /// <summary>
    /// Creates multiple test samples
    /// </summary>
    public static List<Sample> CreateSamples(List<Lab> labs, int samplesPerLab = 2)
    {
        var samples = new List<Sample>();
        var sampleTypes = Enum.GetValues<SampleType>();
        
        foreach (var lab in labs)
        {
            for (int i = 0; i < samplesPerLab; i++)
            {
                var type = sampleTypes[i % sampleTypes.Length];
                samples.Add(CreateSample(lab.Id, type, SampleStatus.Pending, $"Collector {i + 1}"));
            }
        }
        
        return samples;
    }

    /// <summary>
    /// Creates a test result
    /// </summary>
    public static TestResult CreateTestResult(
        Guid sampleId,
        string parameterName,
        double value,
        ComplianceStatus complianceStatus = ComplianceStatus.Pass,
        TestMethod method = TestMethod.Spectrophotometry)
    {
        return new TestResult
        {
            Id = Guid.NewGuid(),
            SampleId = sampleId,
            ParameterName = "Test Parameter",
            Value = value,
            Unit = "mg/L",
            ComplianceStatus = complianceStatus,
            TestMethod = method,
            TechnicianName = "Test Technician",
            TestDate = BaseDate,
            CreatedDate = BaseDate,
            LastModified = BaseDate,
            CreatedBy = "test",
            LastModifiedBy = "test",
            CreatedAt = BaseDate,
            Version = 1,
            IsDeleted = false,
            IsSynced = true
        };
    }

    /// <summary>
    /// Creates multiple test results
    /// </summary>
    public static List<TestResult> CreateTestResults(List<Sample> samples, List<Parameter> parameters)
    {
        var testResults = new List<TestResult>();
        
        foreach (var sample in samples)
        {
            // Create 2-3 test results per sample
            var parameterCount = Math.Min(3, parameters.Count);
            for (int i = 0; i < parameterCount; i++)
            {
                var parameter = parameters[i];
                var value = GenerateTestValue(parameter);
                var compliance = DetermineCompliance(value, parameter);
                
                testResults.Add(CreateTestResult(
                    sample.Id,
                    parameter.Name,
                    value,
                    compliance,
                    TestMethod.Spectrophotometry));
            }
        }
        
        return testResults;
    }

    /// <summary>
    /// Generates a realistic test value for a parameter
    /// </summary>
    private static double GenerateTestValue(Parameter parameter)
    {
        return parameter.Name switch
        {
            "pH" => 7.5,
            "Turbidity" => 3.0,
            "Chlorine" => 1.0,
            "Temperature" => 20.0,
            "Conductivity" => 2000.0,
            _ => 10.0
        };
    }

    /// <summary>
    /// Determines compliance status based on value and parameter thresholds
    /// </summary>
    private static ComplianceStatus DetermineCompliance(double value, Parameter parameter)
    {
        if (parameter.MinValue.HasValue && value < parameter.MinValue.Value)
            return ComplianceStatus.Fail;
        
        if (parameter.MaxValue.HasValue && value > parameter.MaxValue.Value)
            return ComplianceStatus.Fail;
        
        if (parameter.WhoThreshold.HasValue && value > parameter.WhoThreshold.Value)
            return ComplianceStatus.Fail;
        
        if (parameter.MoroccanThreshold.HasValue && value > parameter.MoroccanThreshold.Value)
            return ComplianceStatus.Warning;
        
        return ComplianceStatus.Pass;
    }
}

/// <summary>
/// Container for a complete set of related test data
/// </summary>
public class TestDataSet
{
    public List<Lab> Labs { get; set; } = new();
    public List<Parameter> Parameters { get; set; } = new();
    public List<Sample> Samples { get; set; } = new();
    public List<TestResult> TestResults { get; set; } = new();
}
