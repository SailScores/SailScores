using SailScores.Core.Model;

namespace SailScores.Web.Models.SailScores;

public class FleetWithOptionsViewModel : Core.Model.Fleet
{
    public IEnumerable<BoatClass> BoatClassOptions { get; set; }
    public IEnumerable<Guid> BoatClassIds { get; set; }

    public IEnumerable<Competitor> CompetitorOptions { get; set; }
    public IEnumerable<Guid> CompetitorIds { get; set; }

    public IOrderedEnumerable<BoatClass> CompetitorBoatClassOptions { get; set; }

    public RegattaSummaryViewModel Regatta { get; set; }

    public Guid? RegattaId { get; set; }
}