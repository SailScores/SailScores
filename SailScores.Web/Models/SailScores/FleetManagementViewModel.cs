namespace SailScores.Web.Models.SailScores;

public class FleetManagementViewModel : ClubBaseViewModel
{
    public IList<FleetColumn> Fleets { get; set; } = new List<FleetColumn>();
    public IList<CompetitorRow> Competitors { get; set; } = new List<CompetitorRow>();
}

public class FleetColumn
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string ShortName { get; set; }
}

public class CompetitorRow
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string SailNumber { get; set; }
    public Guid BoatClassId { get; set; }
    public bool IsActive { get; set; }
    public IDictionary<Guid, bool> FleetMembership { get; set; } = new Dictionary<Guid, bool>();
}
