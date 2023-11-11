
namespace SailScores.Database.Entities;

public class RegattaForwarder
{
    public Guid Id { get; set; }

    public String OldClubInitials { get; set; }
    public String OldSeasonUrlName { get; set; }
    public String OldRegattaUrlName { get; set; }

    public Guid NewRegattaId { get; set; }
    public virtual Regatta NewRegatta { get; set; }
    public DateTime Created { get; set; }

}