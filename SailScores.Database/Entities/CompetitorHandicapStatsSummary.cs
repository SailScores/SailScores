namespace SailScores.Database.Entities;

// Keyless result entity for SS_SP_GetSeasonSummaryHandicap.
// Only contains the corrected-time fields plus the season keys needed to join
// with CompetitorStatsSummary in the service layer.
public class CompetitorHandicapStatsSummary
{
    public string SeasonUrlName { get; set; }
    public int? CorrectedRaceCount { get; set; }
    public double? AverageCorrectedRank { get; set; }
    public int? CorrectedBoatsRacedAgainst { get; set; }
    public int? CorrectedBoatsBeat { get; set; }
}
