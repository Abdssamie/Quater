namespace Quater.Shared.Enums;

/// <summary>
/// Current status of a water sample in the testing workflow.
/// </summary>
public enum SampleStatus
{
    /// <summary>
    /// Sample collected but testing not yet completed
    /// </summary>
    Pending,

    /// <summary>
    /// All tests completed for this sample
    /// </summary>
    Completed,

    /// <summary>
    /// Sample archived (historical record, no longer active)
    /// </summary>
    Archived
}
