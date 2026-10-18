using System.Collections.Generic;

namespace SailScores.Core.Scoring.Tiebreaks;

internal static class HighPointSequence
{
    // At the first difference the higher score wins; a missing score counts as 0.
    public static int Compare(IReadOnlyList<decimal> x, IReadOnlyList<decimal> y)
    {
        for (int i = 0; i < System.Math.Max(x.Count, y.Count); i++)
        {
            var xScore = i < x.Count ? x[i] : 0m;
            var yScore = i < y.Count ? y[i] : 0m;
            if (xScore != yScore)
            {
                return yScore.CompareTo(xScore);
            }
        }
        return 0;
    }
}
