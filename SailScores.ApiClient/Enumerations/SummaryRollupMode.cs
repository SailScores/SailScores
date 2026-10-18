using System.ComponentModel.DataAnnotations;

namespace SailScores.Api.Enumerations
{
    // How a summary series uses its child series.
    // Stored as an int: do not renumber existing values.
    public enum SummaryRollupMode
    {
        [Display(Name = "Use child series races")]
        ChildRaces = 0,
        [Display(Name = "Each child series is one race, scored by rank")]
        ChildRankAsRace = 1,
        [Display(Name = "Each child series is one race, scored by its series score")]
        ChildScoreAsRace = 2
    }
}
