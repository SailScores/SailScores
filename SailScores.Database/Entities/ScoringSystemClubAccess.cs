namespace SailScores.Database.Entities;

// Grants a club access to a shared (ClubId == null) scoring system
// whose IsRestrictedToClubs is set.
public class ScoringSystemClubAccess
{
    public Guid ScoringSystemId { get; set; }
    public ScoringSystem ScoringSystem { get; set; }

    public Guid ClubId { get; set; }
    public Club Club { get; set; }
}
