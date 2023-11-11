
namespace SailScores.Database.Entities;

public class CompetitorForwarder
{
    public Guid Id { get; set; }

    public String OldClubInitials { get; set; }
    public String OldCompetitorUrl { get; set; }

    public Guid CompetitorId { get; set; }
    public virtual Competitor NewCompetitor { get; set; }
    public DateTime Created { get; set; }

}