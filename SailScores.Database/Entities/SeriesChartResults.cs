namespace SailScores.Database.Entities;

public class SeriesChartResults
{
    public Guid Id { get; set; }

    public Guid SeriesId { get; set; }
    public bool IsCurrent { get; set; }

    public virtual Series Series { get; set; }
    public string Results { get; set; }
    public DateTime Created { get; set; }

}