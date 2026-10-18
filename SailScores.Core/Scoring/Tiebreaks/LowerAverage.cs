namespace SailScores.Core.Scoring.Tiebreaks;

// The lower average finish ranks ahead (Wawasee 2026). No average ranks last.
public sealed class LowerAverage : ITieBreak
{
    public int Compare(ScoringWorkspace workspace, SeriesCompetitorResults x, SeriesCompetitorResults y) =>
        (x.Average ?? decimal.MaxValue).CompareTo(y.Average ?? decimal.MaxValue);
}
