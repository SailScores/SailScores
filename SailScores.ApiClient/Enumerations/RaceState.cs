using System.ComponentModel.DataAnnotations;

namespace SailScores.Api.Enumerations
{
    public enum RaceState
    {
        [Display(Name = "Raced")]
        Raced = 1,
        [Display(Name = "Scheduled")]
        Scheduled = 2,
        [Display(Name = "Abandoned")]
        Abandoned = 3
    }
}
