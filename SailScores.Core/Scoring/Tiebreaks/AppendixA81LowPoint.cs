using System.Linq;

namespace SailScores.Core.Scoring.Tiebreaks;

// RRS A8.1, low point: compare the scores that weren't discarded, best first; at the first
// difference the better score ranks ahead. If one boat runs out of scores first, the other
// ranks ahead. Scores of 0 are left out unless countZeroScores (First = 0, where a win
// scores 0); scores without a value always are.
public sealed class AppendixA81LowPoint : ITieBreak
{
    private readonly bool _countZeroScores;

    public AppendixA81LowPoint(bool countZeroScores = false)
    {
        _countZeroScores = countZeroScores;
    }

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

    private decimal[] CountedScoresBestFirst(SeriesCompetitorResults results) =>
        results.CalculatedScores.Values
            .Where(s => !s.Discard && (_countZeroScores ? s.ScoreValue.HasValue : (s.ScoreValue ?? 0.0m) != 0.0m))
            .Select(s => s.ScoreValue ?? decimal.MaxValue)
            .OrderBy(s => s)
            .ToArray();
}
