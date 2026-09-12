namespace SailScores.Core.Scoring;

using SailScores.Core.Model;

/// <summary>
/// Represents the allowed count for a score code group, distinguishing between
/// race-based and date-based limitations.
/// </summary>
public class AllowedCountInfo
{
    /// <summary>
    /// Whether the limitation is date-based (as opposed to race-based).
    /// </summary>
    public bool IsDateBased { get; set; }

    /// <summary>
    /// The allowed count. For race-based limitations, this is the number of races.
    /// For date-based limitations, this is the number of distinct dates.
    /// </summary>
    public int AllowedCount { get; set; }

    /// <summary>
    /// The overage selection method (LatestFirst or WorstFirst).
    /// Used by date-based selections to determine which dates to mark as overage.
    /// </summary>
    public ScoreCodeGroupOverageSelection OverageSelectionMethod { get; set; }

    /// <summary>
    /// Creates a race-based allowed count info.
    /// </summary>
    public static AllowedCountInfo RaceBased(int allowedRaceCount)
        => new() { IsDateBased = false, AllowedCount = allowedRaceCount };

    /// <summary>
    /// Creates a date-based allowed count info with overage selection method.
    /// </summary>
    public static AllowedCountInfo DateBased(
        int allowedDateCount,
        ScoreCodeGroupOverageSelection overageSelectionMethod)
        => new()
        {
            IsDateBased = true,
            AllowedCount = allowedDateCount,
            OverageSelectionMethod = overageSelectionMethod
        };
}
