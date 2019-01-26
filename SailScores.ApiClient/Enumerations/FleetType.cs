using System.ComponentModel.DataAnnotations;

namespace SailScores.Api.Enumerations
{
    public enum FleetType
    {
        [Display(Name = "All Boats in Club")]
        AllBoatsInClub = 1,
        [Display(Name = "Selected Classes")]
        SelectedClasses = 2,
        [Display(Name = "Selected Boats")]
        SelectedBoats = 3
    }
}
