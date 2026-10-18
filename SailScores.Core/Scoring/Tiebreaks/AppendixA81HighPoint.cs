using System.Linq;

namespace SailScores.Core.Scoring.Tiebreaks;

// RRS A8.1, high point: compare the scores that weren't discarded, best (highest) first; at
// the first difference the higher score ranks ahead. A missing score value counts as the
// best possible. If one boat has fewer scores, the other ranks ahead.
public sealed class AppendixA81HighPoint : ITieBreak
{
    public int Compare(ScoringWorkspace workspace, SeriesCompetitorResults x, SeriesCompetitorResults y)
    {
        var xScores = CountedScoresBestFirst(x);
        var yScores = CountedScoresBestFirst(y);

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
                return yScores[i].CompareTo(xScores[i]);
            }
        }
        return xScores.Length < yScores.Length ? 1 : 0;
    }

    private static decimal[] CountedScoresBestFirst(SeriesCompetitorResults results) =>
        results.CalculatedScores.Values
            .Where(s => !s.Discard)
            .Select(s => s.ScoreValue ?? decimal.MaxValue)
            .OrderByDescending(s => s)
            .ToArray();
}
