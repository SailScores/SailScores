namespace SailScores.Web.Models.SailScores;

public class FleetCreateViewModel : Core.Model.Fleet
{
    public IEnumerable<Guid> BoatClassIds { get; set; }
    public Guid? RegattaId { get; set; }

}