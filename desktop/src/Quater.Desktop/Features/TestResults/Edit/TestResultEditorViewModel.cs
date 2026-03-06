using CommunityToolkit.Mvvm.ComponentModel;
using Quater.Desktop.Api.Model;

namespace Quater.Desktop.Features.TestResults.Edit;

public sealed partial class TestResultEditorViewModel : ObservableObject
{
    [ObservableProperty]
    private Guid? _editingResultId;

    [ObservableProperty]
    private Guid _sampleId;

    [ObservableProperty]
    private string _parameterName = string.Empty;

    [ObservableProperty]
    private double _value;

    [ObservableProperty]
    private string _unit = string.Empty;

    [ObservableProperty]
    private DateTimeOffset? _testDate = DateTimeOffset.UtcNow;

    [ObservableProperty]
    private string _technicianName = string.Empty;

    [ObservableProperty]
    private TestMethod _testMethod = TestMethod.NUMBER_0;

    [ObservableProperty]
    private int _version;

    public IEnumerable<TestMethod> TestMethods => Enum.GetValues<TestMethod>();

    public bool IsEditMode => EditingResultId.HasValue;

    public void InitializeForCreate(Guid sampleId)
    {
        EditingResultId = null;
        SampleId = sampleId;
        ParameterName = string.Empty;
        Value = 0;
        Unit = string.Empty;
        TestDate = DateTimeOffset.UtcNow;
        TechnicianName = string.Empty;
        TestMethod = TestMethod.NUMBER_0;
        Version = 0;
    }

    public void InitializeForEdit(List.TestResultListItem item)
    {
        EditingResultId = item.Id;
        SampleId = item.SampleId;
        ParameterName = item.ParameterName;
        Value = item.Value;
        Unit = item.Unit;
        TestDate = new DateTimeOffset(item.TestDate, TimeSpan.Zero);
        TechnicianName = item.TechnicianName;
        TestMethod = item.TestMethod;
        Version = item.Version;
    }

    public bool TryBuildCreateDto(out CreateTestResultDto? dto)
    {
        dto = null;
        if (!TryBuildCommon(out var testDateUtc))
        {
            return false;
        }

        dto = new CreateTestResultDto(
            sampleId: SampleId,
            parameterName: ParameterName.Trim(),
            value: Value,
            unit: Unit.Trim(),
            testDate: testDateUtc,
            technicianName: TechnicianName.Trim(),
            testMethod: TestMethod);
        return true;
    }

    public bool TryBuildUpdateDto(out UpdateTestResultDto? dto)
    {
        dto = null;
        if (!TryBuildCommon(out var testDateUtc))
        {
            return false;
        }

        dto = new UpdateTestResultDto(
            parameterName: ParameterName.Trim(),
            value: Value,
            unit: Unit.Trim(),
            testDate: testDateUtc,
            technicianName: TechnicianName.Trim(),
            testMethod: TestMethod,
            varVersion: Version);
        return true;
    }

    private bool TryBuildCommon(out DateTime testDateUtc)
    {
        testDateUtc = default;

        if (SampleId == Guid.Empty ||
            string.IsNullOrWhiteSpace(ParameterName) ||
            string.IsNullOrWhiteSpace(Unit) ||
            string.IsNullOrWhiteSpace(TechnicianName) ||
            !TestDate.HasValue)
        {
            return false;
        }

        testDateUtc = TestDate.Value.UtcDateTime;
        return true;
    }
}
