namespace SailScores.Core.Scoring.Tiebreaks;

// Low point: the lower series total ranks ahead. No total (unranked) ranks last.
public sealed class LowestTotal : ITieBreak
{
    private readonly bool _zeroTotalRanksLast;

    // zeroTotalRanksLast: a total of 0 counts as no total. Quirk (bug): under Appendix A,
    // First = 0, a boat that won every counted race totals 0 and ranks last
    // (docs/Scoring-Quirks.md).
    public LowestTotal(bool zeroTotalRanksLast)
    {
        _zeroTotalRanksLast = zeroTotalRanksLast;
    }

    public int Compare(ScoringWorkspace workspace, SeriesCompetitorResults x, SeriesCompetitorResults y) =>
        TotalToCompare(x).CompareTo(TotalToCompare(y));

    private decimal TotalToCompare(SeriesCompetitorResults results)
    {
        var total = results.TotalScore ?? decimal.MaxValue;
        return _zeroTotalRanksLast && total == 0 ? decimal.MaxValue : total;
    }
}
