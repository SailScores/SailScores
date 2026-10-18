using System.Linq;

namespace SailScores.Core.Scoring.Tiebreaks;

// RRS A8.1, low point: compare the scores that weren't discarded, best first; at the first
// difference the better score ranks ahead. If one boat runs out of scores first, the other
// ranks ahead.
//
// Quirk: scores of 0 are left out, so under First = 0 wins don't count here
// (docs/Scoring-Quirks.md).
public sealed class AppendixA81LowPoint : ITieBreak
{
    public int Compare(ScoringWorkspace workspace, SeriesCompetitorResults x, SeriesCompetitorResults y)
    {
        var xScores = CountedScoresBestFirst(x);
        var yScores = CountedScoresBestFirst(y);

        for (int i = 0; i < xScores.Length; i++)
        {
            if (yScores.Length <= i)
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

    private static decimal[] CountedScoresBestFirst(SeriesCompetitorResults results) =>
        results.CalculatedScores.Values
            .Where(s => !s.Discard && (s.ScoreValue ?? 0.0m) != 0.0m)
            .Select(s => s.ScoreValue ?? decimal.MaxValue)
            .OrderBy(s => s)
            .ToArray();
}
