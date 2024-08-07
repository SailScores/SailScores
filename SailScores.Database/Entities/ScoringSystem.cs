namespace SailScores.Database.Entities;

public class ScoringSystem
{
    public Guid Id { get; set; }

    public Guid? ClubId { get; set; }

    public IList<Club> DefaultForClubs { get; set; }

    public Guid? ParentSystemId { get; set; }

    [StringLength(100)]
    public String Name { get; set; }

    public String DiscardPattern { get; set; }

    public IList<ScoreCode> ScoreCodes { get; set; }

    [ForeignKey("ParentSystemId")]
    public ScoringSystem ParentSystem { get; set; }

    public Decimal? ParticipationPercent { get; set; }
    public bool? IsSiteDefault { get; set; }
}