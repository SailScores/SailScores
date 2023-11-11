
namespace SailScores.Database.Entities;

public class SeriesForwarder
{
    public Guid Id { get; set; }

    public String OldClubInitials { get; set; }
    public String OldSeasonUrlName { get; set; }
    public String OldSeriesUrlName { get; set; }

    public Guid NewSeriesId { get; set; }
    public virtual Series NewSeries { get; set; }
    public DateTime Created { get; set; }

}