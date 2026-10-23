using System;
using System.Linq;

namespace SailScores.Core.Scoring.Tiebreaks;

// RRS Appendix B A8.1 (windsurfing): compare the excluded (discarded) scores, best first; at
// the first difference the better score ranks ahead. Boats that excluded different numbers of
// scores are compared as far as the shorter list goes; no difference there leaves the tie for
// the next step. A missing score value counts as decimal.MaxValue.
public sealed class AppendixBExcludedScores : ITieBreak
{
    public int Compare(ScoringWorkspace workspace, SeriesCompetitorResults x, SeriesCompetitorResults y)
    {
        var xScores = ExcludedBestFirst(x);
        var yScores = ExcludedBestFirst(y);

        var length = Math.Min(xScores.Length, yScores.Length);
        for (int i = 0; i < length; i++)
        {
            if (xScores[i] != yScores[i])
            {
                return xScores[i] < yScores[i] ? -1 : 1;
            }
        }
        return 0;
    }

    private static decimal[] ExcludedBestFirst(SeriesCompetitorResults results) =>
        results.CalculatedScores.Values
            .Where(s => s.Discard)
            .Select(s => s.ScoreValue ?? decimal.MaxValue)
            .OrderBy(s => s)
            .ToArray();
}
