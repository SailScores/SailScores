using System.Collections.Generic;
using System.Linq;
using SailScores.Core.Model;

namespace SailScores.Core.Scoring.Totals;

// VARC 2026: the total is the sum of scores not discarded; higher wins. Also sets the total
// including discards, and the results' number of discards to the most any competitor has
// (it varies by competitor).
public sealed class HighPointSum : ITotalRule
{
    public void Apply(ScoringWorkspace workspace, SeriesResults results, IEnumerable<Score> scores)
    {
        // Tells the UI not to treat the total as a place.
        results.IsPercentSystem = true;
        results.LowerScoreWins = false;

        foreach (var compResults in results.Results.Values)
        {
            compResults.TotalScore = compResults.CalculatedScores.Values
                .Sum(s => s.Discard ? 0m : s.ScoreValue ?? 0m);
            compResults.TotalScoreIncludingDiscards = compResults.CalculatedScores.Values
                .Sum(s => s.ScoreValue ?? 0m);
            compResults.PointsEarned = compResults.TotalScore;
        }

        results.NumberOfDiscards = results.Results.Values
            .Select(r => r.CalculatedScores.Values.Count(s => s.Discard))
            .DefaultIfEmpty(0)
            .Max();
    }
}
