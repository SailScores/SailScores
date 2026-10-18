using System.Collections.Generic;
using System.Linq;
using SailScores.Core.Model;

namespace SailScores.Core.Scoring.Totals;

// Top X: the total is the sum of the competitor's best X scores. X is stored in the system's
// ParticipationPercent field (5 if blank), a stopgap from when the system was added.
// Higher wins.
public sealed class TopXBestScores : ITotalRule
{
    public void Apply(ScoringWorkspace workspace, SeriesResults results, IEnumerable<Score> scores)
    {
        // Tells the UI not to treat the total as a place.
        results.IsPercentSystem = true;
        results.LowerScoreWins = false;
        results.PercentRequired = 0;
        var racesToInclude = (int)(workspace.System.EffectiveParticipationPercent ?? 5m);

        foreach (var comp in results.Competitors)
        {
            var compResults = results.Results[comp];
            var total = compResults.CalculatedScores
                .OrderByDescending(s => s.Value.ScoreValue)
                .Take(racesToInclude)
                .Sum(s => s.Value.ScoreValue ?? 0.0m);

            compResults.PointsEarned = total;
            compResults.TotalScore = total;
        }
    }
}
