using System.ComponentModel.DataAnnotations;

namespace SailScores.Database.Entities;

public enum PermissionLevel
{
    [Display(Name = "Club Administrator")]
    ClubAdministrator = 0,
    [Display(Name = "Series Scorekeeper")]
    SeriesScorekeeper = 1,
    [Display(Name = "Race Scorekeeper")]
    RaceScorekeeper = 2
}
