using System.Linq;

namespace SailScores.Core.Scoring.Tiebreaks;

// PWA tiebreak (B): compare all scores, discards included, best first; at the first
// difference the better score ranks ahead. If one boat has more scores, it ranks ahead.
public sealed class PwaBetterScoresIncludingDiscards : ITieBreak
{
    public int Compare(ScoringWorkspace workspace, SeriesCompetitorResults x, SeriesCompetitorResults y)
    {
        var xScores = BestFirst(x);
        var yScores = BestFirst(y);

        for (int i = 0; i < xScores.Length; i++)
        {
            // As written before the refactor: "<" lets y run out one score early, which
            // can't happen as every competitor has a score in every race.
            if (yScores.Length < i)
            {
                return -1;
            }
            if (xScores[i] != yScores[i])
            {
                return xScores[i] < yScores[i] ? -1 : 1;
            }
        }
        return xScores.Length < yScores.Length ? 1 : 0;
    }

    private static decimal[] BestFirst(SeriesCompetitorResults results) =>
        results.CalculatedScores.Values
            .Select(s => s.ScoreValue ?? decimal.MaxValue)
            .OrderBy(s => s)
            .ToArray();
}
