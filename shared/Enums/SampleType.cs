namespace Quater.Shared.Enums;

/// <summary>
/// Types of water samples that can be collected and tested.
/// </summary>
public enum SampleType
{
    /// <summary>
    /// Drinking water sample (potable water for human consumption)
    /// </summary>
    DrinkingWater,

    /// <summary>
    /// Wastewater sample (sewage or industrial effluent)
    /// </summary>
    Wastewater,

    /// <summary>
    /// Surface water sample (rivers, lakes, streams)
    /// </summary>
    SurfaceWater,

    /// <summary>
    /// Groundwater sample (wells, aquifers)
    /// </summary>
    Groundwater,

    /// <summary>
    /// Industrial water sample (process water, cooling water)
    /// </summary>
    IndustrialWater
}
