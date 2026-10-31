using System.ComponentModel.DataAnnotations;

namespace SailScores.Api.Enumerations;

// Vertical lines between race columns in series results.
public enum ResultsColumnSeparator
{
    [Display(Name = "None")]
    None = 0,

    [Display(Name = "Between every column")]
    EveryColumn = 1,

    [Display(Name = "Between race days")]
    RaceDays = 2,

    // Summary series: where the columns change from one child series to the next.
    [Display(Name = "Between child series")]
    ChildSeries = 3
}
