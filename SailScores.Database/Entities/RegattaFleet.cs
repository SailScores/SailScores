namespace SailScores.Database.Entities;

public class RegattaFleet
{
    public Guid RegattaId { get; set; }
    public Regatta Regatta { get; set; }

    public Guid FleetId { get; set; }
    public Fleet Fleet { get; set; }
}