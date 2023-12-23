using SailScores.Api.Enumerations;
using SailScores.Core.Model;
using System.ComponentModel.DataAnnotations;

namespace SailScores.Web.Models.SailScores;

public class FleetWithOptionsViewModel
{

    public Guid Id { get; set; }
    public Guid ClubId { get; set; }

    // Short Name, unique to the fleet in this club. Can be used in Urls.
    // Example: 2019DieHardMCs
    [StringLength(30)]
    [Display(Name = "Short Name")]
    public String ShortName { get; set; }

    // Full display Name
    // Example: 2019 Die Hard MC Scows
    [Required]
    [StringLength(200)]
    public String Name { get; set; }

    // Short Alias Name, does not need to be unique: used for display in regattas
    // Example: MC Scows
    [StringLength(30)]
    [Display(Name = "Nickname")]
    public String NickName { get; set; }

    [StringLength(2000)]
    public String Description { get; set; }


    [Display(Name = "Active")]
    public bool IsActive { get; set; }

    [Required]
    [Display(Name = "Fleet Type")]
    public FleetType FleetType { get; set; }

    [Display(Name = "Boat Classes")]
    public IList<BoatClass> BoatClasses { get; set; }
    public IList<Competitor> Competitors { get; set; }


    // above are properties shared with Core.Model.Fleet

    public String SuggestedFullName { get; set; }
    public IEnumerable<BoatClass> BoatClassOptions { get; set; }
    public IEnumerable<Guid> BoatClassIds { get; set; }

    public IEnumerable<Competitor> CompetitorOptions { get; set; }
    public IEnumerable<Guid> CompetitorIds { get; set; }

    public IOrderedEnumerable<BoatClass> CompetitorBoatClassOptions { get; set; }

    public RegattaSummaryViewModel Regatta { get; set; }

    public Guid? RegattaId { get; set; }
}