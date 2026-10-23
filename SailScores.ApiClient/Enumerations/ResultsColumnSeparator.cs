using System.ComponentModel.DataAnnotations;

namespace SailScores.Api.Enumerations;

// Vertical lines between race columns in series results.
public enum ResultsColumnSeparator
{
    [Display(Name = "None")]
    None = 0,

    [Display(Name = "Between every race or child series")]
    EveryColumn = 1,

    [Display(Name = "Between race days")]
    RaceDays = 2
}
