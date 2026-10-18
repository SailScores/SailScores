using System.ComponentModel.DataAnnotations;

namespace SailScores.Api.Enumerations
{
    // How a scoring system decides which scores are discarded.
    // Stored as an int: do not renumber existing values.
    public enum DiscardMethod
    {
        [Display(Name = "By number of races (discard pattern)")]
        RaceCountPattern = 0,
        // The discard pattern is applied to each day's races separately.
        [Display(Name = "Per day (discard pattern applied to each day)")]
        PerDay = 1,
        // Summary series: keeps the discards each child series made.
        [Display(Name = "Keep child series discards")]
        PreserveChildSeriesDiscards = 2,
        // Summary series: the discard pattern is applied to each child series' races
        // separately. Other series treat all their races as one group.
        [Display(Name = "Per child series (discard pattern applied to each child series)")]
        PerChildSeries = 3,
        // PreserveChildSeriesDiscards in a summary series that uses its child series' races;
        // RaceCountPattern in any other series.
        [Display(Name = "Keep child series discards in summary series; discard pattern otherwise")]
        ChildSeriesDiscardsOrPattern = 4
    }
}
