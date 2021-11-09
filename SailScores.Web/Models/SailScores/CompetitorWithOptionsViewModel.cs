using SailScores.Core.Model;

namespace SailScores.Web.Models.SailScores;

public class CompetitorWithOptionsViewModel : Core.Model.Competitor
{
    public IOrderedEnumerable<BoatClass> BoatClassOptions { get; set; }

    public IList<FleetSummary> FleetOptions { get; set; }

    public IList<Guid> FleetIds { get; set; }
}