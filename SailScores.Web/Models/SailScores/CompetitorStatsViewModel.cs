using SailScores.Core.Model;
using System.ComponentModel.DataAnnotations;

namespace SailScores.Web.Models.SailScores;

public class CompetitorStatsViewModel
{
    public Guid Id { get; set; }

    [StringLength(200)]
    public String Name { get; set; }

    [Display(Name = "Sail Number")]
    [StringLength(20)]
    public String SailNumber { get; set; }

    [Display(Name = "Boat Name")]
    [StringLength(200)]
    public String BoatName { get; set; }

    [Display(Name = "Home Club Name")]
    [StringLength(200)]
    public String HomeClubName { get; set; }

    public IList<CompetitorSeasonStats> SeasonStats { get; internal set; }

    public bool ClubHasHandicapScoring { get; set; }
    public bool ClubHasDefaultHandicapSystem { get; set; }
    public bool CompetitorHasRatingForDefaultSystem { get; set; }
    public string HandicapSystemName { get; set; }

    public override string ToString()
    {
        return BoatName + " : " + Name + " : " + SailNumber + " : " + Id;
    }
}
