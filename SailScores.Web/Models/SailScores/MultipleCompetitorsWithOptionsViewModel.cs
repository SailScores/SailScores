using SailScores.Core.Model;
using System.ComponentModel.DataAnnotations;

namespace SailScores.Web.Models.SailScores;

public class MultipleCompetitorsWithOptionsViewModel
{
    public IOrderedEnumerable<BoatClass> BoatClassOptions { get; set; }

    [Required]
    [Display(Name = "Boat Class")]
    public Guid BoatClassId { get; set; }
    public IList<FleetSummary> FleetOptions { get; set; }

    public IList<Guid> FleetIds { get; set; }

    public IList<CompetitorViewModel> Competitors { get; set; }

}