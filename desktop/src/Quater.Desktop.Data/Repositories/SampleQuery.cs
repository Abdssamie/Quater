using Quater.Shared.Enums;

namespace Quater.Desktop.Data.Repositories;

public sealed class SampleQuery
{
    public SampleStatus? Status { get; init; }
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public string SearchText { get; init; } = string.Empty;
    public Guid? LabId { get; init; }
}
