using CommunityToolkit.Mvvm.ComponentModel;
using Quater.Desktop.Api.Model;
using Quater.Shared.Models;

namespace Quater.Desktop.Features.Samples.Edit;

using SharedSampleStatus = Quater.Shared.Enums.SampleStatus;
using SharedSampleType = Quater.Shared.Enums.SampleType;

public sealed partial class SampleEditorViewModel : ObservableObject
{
    [ObservableProperty]
    private Guid? _editingSampleId;

    [ObservableProperty]
    private SharedSampleType _type = SharedSampleType.DrinkingWater;

    [ObservableProperty]
    private double _locationLatitude = 0;

    [ObservableProperty]
    private double _locationLongitude = 0;

    [ObservableProperty]
    private string _locationDescription = string.Empty;

    [ObservableProperty]
    private string _locationHierarchy = string.Empty;

    [ObservableProperty]
    private DateTimeOffset? _collectionDate = DateTimeOffset.UtcNow;

    [ObservableProperty]
    private string _collectorName = string.Empty;

    [ObservableProperty]
    private string _notes = string.Empty;

    [ObservableProperty]
    private SharedSampleStatus _status = SharedSampleStatus.Pending;

    [ObservableProperty]
    private int _version;

    public IEnumerable<SharedSampleType> SampleTypes => Enum.GetValues<SharedSampleType>();
    public IEnumerable<SharedSampleStatus> SampleStatuses => Enum.GetValues<SharedSampleStatus>();
    public bool IsEditMode => EditingSampleId.HasValue;

    public void InitializeForCreate()
    {
        EditingSampleId = null;
        Type = SharedSampleType.DrinkingWater;
        LocationLatitude = 0;
        LocationLongitude = 0;
        LocationDescription = string.Empty;
        LocationHierarchy = string.Empty;
        CollectionDate = DateTimeOffset.UtcNow;
        CollectorName = string.Empty;
        Notes = string.Empty;
        Status = SharedSampleStatus.Pending;
        Version = 0;
    }

    public void InitializeForEdit(Sample sample)
    {
        EditingSampleId = sample.Id;
        Type = sample.Type;
        LocationLatitude = sample.Location.Latitude;
        LocationLongitude = sample.Location.Longitude;
        LocationDescription = sample.Location.Description ?? string.Empty;
        LocationHierarchy = sample.Location.Hierarchy ?? string.Empty;
        CollectionDate = new DateTimeOffset(sample.CollectionDate, TimeSpan.Zero);
        CollectorName = sample.CollectorName;
        Notes = sample.Notes ?? string.Empty;
        Status = sample.Status;
        Version = sample.RowVersion is { Length: >= 4 }
            ? BitConverter.ToInt32(sample.RowVersion, 0)
            : 0;
    }

    public bool TryBuildCreateDto(Guid labId, out CreateSampleDto? dto)
    {
        dto = null;
        if (labId == Guid.Empty || !TryBuildCommon(out var collectionDateUtc))
        {
            return false;
        }

        dto = new CreateSampleDto(
            type: (Quater.Desktop.Api.Model.SampleType)Type,
            locationLatitude: LocationLatitude,
            locationLongitude: LocationLongitude,
            locationDescription: TrimToNull(LocationDescription),
            locationHierarchy: TrimToNull(LocationHierarchy),
            collectionDate: collectionDateUtc,
            collectorName: CollectorName.Trim(),
            notes: TrimToNull(Notes),
            labId: labId);
        return true;
    }

    public bool TryBuildUpdateDto(out UpdateSampleDto? dto)
    {
        dto = null;
        if (!TryBuildCommon(out var collectionDateUtc))
        {
            return false;
        }

        dto = new UpdateSampleDto(
            type: (Quater.Desktop.Api.Model.SampleType)Type,
            locationLatitude: LocationLatitude,
            locationLongitude: LocationLongitude,
            locationDescription: TrimToNull(LocationDescription),
            locationHierarchy: TrimToNull(LocationHierarchy),
            collectionDate: collectionDateUtc,
            collectorName: CollectorName.Trim(),
            notes: TrimToNull(Notes),
            status: (Quater.Desktop.Api.Model.SampleStatus)Status,
            varVersion: Version);
        return true;
    }

    private bool TryBuildCommon(out DateTime collectionDateUtc)
    {
        collectionDateUtc = default;

        if (string.IsNullOrWhiteSpace(CollectorName) ||
            !CollectionDate.HasValue ||
            LocationLatitude is < -90 or > 90 ||
            LocationLongitude is < -180 or > 180)
        {
            return false;
        }

        collectionDateUtc = CollectionDate.Value.UtcDateTime;
        return true;
    }

    private static string? TrimToNull(string value)
    {
        var trimmed = value.Trim();
        return trimmed.Length == 0 ? null : trimmed;
    }
}
