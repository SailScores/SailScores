namespace SailScores.Core.Scoring.Tiebreaks;

// High point: the higher series total ranks ahead. No total (unranked) ranks last.
public sealed class HighestTotal : ITieBreak
{
    public int Compare(ScoringWorkspace workspace, SeriesCompetitorResults x, SeriesCompetitorResults y) =>
        (y.TotalScore ?? -1).CompareTo(x.TotalScore ?? -1);
}
