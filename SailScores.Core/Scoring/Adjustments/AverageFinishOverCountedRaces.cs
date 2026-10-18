using System;
using System.Linq;
using SailScores.Api.Enumerations;

namespace SailScores.Core.Scoring.Adjustments;

// Wawasee 2026: average-scored codes (DNC, excused, breakdown) score the boat's average
// finish over races that count. The AVE formulas have already scored them, over every race.
// Child series scores are left as they are.
public sealed class AverageFinishOverCountedRaces : IScoreAdjustment
{
    public void Apply(ScoringWorkspace workspace, SeriesResults results, SeriesCompetitorResults competitor)
    {
        var average = For(workspace, competitor);
        foreach (var race in results.SailedRaces)
        {
            var score = competitor.CalculatedScores[race];
            if (workspace.Host.IsAverage(score.RawScore.Code) && !score.IsPresetScore)
            {
                score.ScoreValue = average;
            }
        }
    }

    // Mean of the scores that aren't averages or 0, in races that count, rounded to one
    // decimal like other average codes. Null when there are none.
    public static decimal? For(ScoringWorkspace workspace, SeriesCompetitorResults competitor)
    {
        var finishes = competitor.CalculatedScores
            .Where(kv => !workspace.RacesNotCounted.Contains(kv.Key)
                && (kv.Key.State ?? RaceState.Raced) is RaceState.Raced or RaceState.Preliminary
                && !workspace.Host.IsAverage(kv.Value.RawScore.Code)
                && (kv.Value.ScoreValue ?? 0m) != 0m)
            .Select(kv => kv.Value.ScoreValue.Value)
            .ToList();
        return finishes.Count == 0
            ? null
            : Math.Round(finishes.Average(), 1, MidpointRounding.AwayFromZero);
    }
}
