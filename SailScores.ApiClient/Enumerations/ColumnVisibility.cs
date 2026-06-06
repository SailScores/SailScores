using System.ComponentModel.DataAnnotations;

namespace SailScores.Api.Enumerations;

public enum ColumnVisibility
{
    [Display(Name = "Hidden")]
    Hidden = 10,

    [Display(Name = "Always")]
    Always = 1,

    [Display(Name = "On Larger Screens")]
    OnLargerScreens = 2
}
