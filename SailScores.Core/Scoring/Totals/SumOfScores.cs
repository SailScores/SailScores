using System.Collections.Generic;
using System.Linq;
using SailScores.Core.Model;

namespace SailScores.Core.Scoring.Totals;

// Low point: the total is the sum of scores not discarded. Also sets the total including
// discards.
public sealed class SumOfScores : ITotalRule
{
    public void Apply(ScoringWorkspace workspace, SeriesResults results, IEnumerable<Score> scores)
    {
        foreach (var comp in results.Competitors)
        {
            var compResults = results.Results[comp];

            compResults.TotalScore = compResults.CalculatedScores.Values
                .Sum(s => !s.Discard ? (s.ScoreValue ?? 0.0m) : 0.0m);

            compResults.TotalScoreIncludingDiscards = compResults.CalculatedScores.Values
                .Sum(s => s.ScoreValue ?? 0.0m);
        }
    }
}
