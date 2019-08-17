using System.ComponentModel.DataAnnotations;

namespace SailScores.Api.Enumerations
{
    public enum TrendOption
    {
        [Display(Name = "None")]
        None = 1,
        [Display(Name = "Previous Race")]
        PreviousRace = 2,
        [Display(Name = "Previous Day")]
        PreviousDay = 3,
        [Display(Name = "Previous Week")]
        PreviousWeek = 4
    }
}
