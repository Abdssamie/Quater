namespace Quater.Backend.Core.Enums;

/// <summary>
/// Laboratory methods used for water quality testing.
/// </summary>
public enum TestMethod
{
    /// <summary>
    /// Titration method (volumetric analysis)
    /// </summary>
    Titration,

    /// <summary>
    /// Spectrophotometry method (light absorption measurement)
    /// </summary>
    Spectrophotometry,

    /// <summary>
    /// Chromatography method (separation and analysis)
    /// </summary>
    Chromatography,

    /// <summary>
    /// Microscopy method (visual examination under microscope)
    /// </summary>
    Microscopy,

    /// <summary>
    /// Electrode method (electrochemical measurement, e.g., pH meter)
    /// </summary>
    Electrode,

    /// <summary>
    /// Culture method (microbiological growth and counting)
    /// </summary>
    Culture,

    /// <summary>
    /// Other testing method not listed above
    /// </summary>
    Other
}
