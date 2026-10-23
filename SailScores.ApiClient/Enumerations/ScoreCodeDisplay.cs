using System.ComponentModel.DataAnnotations;

namespace SailScores.Api.Enumerations;

// How a score code is shown in a series results cell.
public enum ScoreCodeDisplay
{
    [Display(Name = "Code and points")]
    CodeAndPoints = 0,

    [Display(Name = "Code only")]
    CodeOnly = 1,

    [Display(Name = "Hidden")]
    Hidden = 2
}
