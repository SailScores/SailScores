using System.ComponentModel.DataAnnotations;

namespace SailScores.Api.Enumerations;

// Text color for scores in series results, such as discards.
public enum ResultsTextColor
{
    [Display(Name = "Normal")]
    Default = 0,

    [Display(Name = "Red")]
    Red = 1,

    [Display(Name = "Blue")]
    Blue = 2,

    [Display(Name = "Green")]
    Green = 3,

    [Display(Name = "Orange")]
    Orange = 4
}
