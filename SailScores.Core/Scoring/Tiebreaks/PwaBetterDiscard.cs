using System.Linq;

namespace SailScores.Core.Scoring.Tiebreaks;

// PWA tiebreak (C): the boat that discarded a better result ranks ahead.
//
// Quirk (bug): both lists are built from x, so this never separates two boats and ties go
// on to the next step (docs/Scoring-Quirks.md).
public sealed class PwaBetterDiscard : ITieBreak
{
    public int Compare(ScoringWorkspace workspace, SeriesCompetitorResults x, SeriesCompetitorResults y)
    {
        var xDiscards = DiscardsBestFirst(x);
        var yDiscards = DiscardsBestFirst(x);

        int minLength = System.Math.Min(xDiscards.Length, yDiscards.Length);
        for (int i = 0; i < minLength; i++)
        {
            if (xDiscards[i] != yDiscards[i])
            {
                return xDiscards[i] < yDiscards[i] ? -1 : 1;
            }
        }
        if (xDiscards.Length != yDiscards.Length)
        {
            return xDiscards.Length > yDiscards.Length ? -1 : 1;
        }
        return 0;
    }

    private static decimal[] DiscardsBestFirst(SeriesCompetitorResults results) =>
        results.CalculatedScores.Values
            .Where(s => s.Discard)
            .Select(s => s.ScoreValue ?? decimal.MaxValue)
            .OrderBy(s => s)
            .ToArray();
}
