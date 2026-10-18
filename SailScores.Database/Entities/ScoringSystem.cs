using SailScores.Api.Enumerations;

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

    public IList<ScoreCodeGroup> ScoreCodeGroups { get; set; }

    [ForeignKey("ParentSystemId")]
    public ScoringSystem ParentSystem { get; set; }

    public Decimal? ParticipationPercent { get; set; }
    public bool? IsSiteDefault { get; set; }

    // Only set on base systems (ParentSystemId == null). Child systems use their root's value.
    public ScoringCalculatorType? CalculatorType { get; set; }

    // JSON for calculator-specific settings (Core.Model.ScoringSystemSettings).
    // Null values inherit from the parent system.
    public String Settings { get; set; }

    // Null inherits from the parent; null at the root means RaceCountPattern.
    public DiscardMethod? DiscardMethod { get; set; }

    // Only used on shared systems (ClubId == null). When true, only the clubs
    // in ClubAccess can choose this system as a parent; otherwise all clubs can.
    public bool IsRestrictedToClubs { get; set; }

    public IList<ScoringSystemClubAccess> ClubAccess { get; set; }
}
