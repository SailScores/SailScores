namespace SailScores.Database.Entities;

public class RegattaSeries
{
    public Guid RegattaId { get; set; }
    public Regatta Regatta { get; set; }

    public Guid SeriesId { get; set; }
    public Series Series { get; set; }
}