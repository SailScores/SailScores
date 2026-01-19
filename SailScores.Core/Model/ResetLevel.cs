namespace SailScores.Core.Model;

/// <summary>
/// Defines the level of data to clear when resetting a club.
/// </summary>
public enum ResetLevel
{
    /// <summary>
    /// Clear races, scores, series, and regattas.
    /// Preserves competitors, fleets, boat classes, seasons, and scoring systems.
    /// </summary>
    RacesAndSeries = 1,

    /// <summary>
    /// Clear races, scores, series, regattas, and competitors.
    /// Preserves fleets, boat classes, seasons, and scoring systems.
    /// </summary>
    RacesSeriesAndCompetitors = 2,

    /// <summary>
    /// Full reset: Clear all data and reset scoring systems to defaults.
    /// Preserves only club name, initials, and website URL.
    /// </summary>
    FullReset = 3
}
