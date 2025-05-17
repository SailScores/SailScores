namespace SailScores.Database.Entities;

public class Competitor
{
    public Guid Id { get; set; }
    public Guid ClubId { get; set; }
    [StringLength(200)]
    public String Name { get; set; }
    [StringLength(20)]
    public String SailNumber { get; set; }
    [StringLength(20)]
    public String AlternativeSailNumber { get; set; }
    [StringLength(200)]
    public String BoatName { get; set; }

    [StringLength(200)]
    public String HomeClubName { get; set; }

    [StringLength(2000)]
    public String Notes { get; set; }

    // Will the competitor be available in creating or editting a race?
    public bool? IsActive { get; set; }

    public Guid BoatClassId { get; set; }
    public BoatClass BoatClass { get; set; }
    public IList<CompetitorFleet> CompetitorFleets { get; set; }
    public IList<Score> Scores { get; set; }

    public IList<CompetitorHistory> History { get; set; }

    [StringLength(20)]
    public String UrlName { get; set; }

    // A fallback url, used in the event of duplicate sailnumbers, usually.
    // it should be initialized on competitor create, and not change.
    [StringLength(20)]
    public String UrlId { get; set; }

    public DateTime? Created { get; set; }

    public override string ToString()
    {
        return BoatName + " : " + Name + " : " + SailNumber + " : " + Id;
    }

}