using System.Linq;

namespace SailScores.Core.Scoring.Tiebreaks;

// Low point: the lower series total ranks ahead. No total (unranked) ranks last.
public sealed class LowestTotal : ITieBreak
{
    private readonly bool _zeroTotalRanksLast;
    private readonly bool _zeroFromScoresCounts;

    // zeroTotalRanksLast: a total of 0 counts as no total. zeroFromScoresCounts: except when
    // the boat has a counted score, so a 0 total made of 0 scores (First = 0 wins) ranks
    // first.
    public LowestTotal(bool zeroTotalRanksLast, bool zeroFromScoresCounts = false)
    {
        _zeroTotalRanksLast = zeroTotalRanksLast;
        _zeroFromScoresCounts = zeroFromScoresCounts;
    }

    public int Compare(ScoringWorkspace workspace, SeriesCompetitorResults x, SeriesCompetitorResults y) =>
        TotalToCompare(x).CompareTo(TotalToCompare(y));

    private decimal TotalToCompare(SeriesCompetitorResults results)
    {
        var total = results.TotalScore ?? decimal.MaxValue;
        if (_zeroTotalRanksLast && total == 0 && !(_zeroFromScoresCounts && HasCountedScore(results)))
        {
            return decimal.MaxValue;
        }
        return total;
    }

    private static bool HasCountedScore(SeriesCompetitorResults results) =>
        results.CalculatedScores.Values.Any(s => !s.Discard && s.ScoreValue.HasValue);
}
