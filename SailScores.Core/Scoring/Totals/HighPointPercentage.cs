using System;
using System.Collections.Generic;
using System.Linq;
using SailScores.Api.Enumerations;
using SailScores.Core.Model;

namespace SailScores.Core.Scoring.Totals;

// High point percentage: the total is the points earned in races not discarded, as a
// percentage of the points possible there (the boats that came to the start in races the
// competitor sailed and didn't discard). A competitor who sailed fewer than
// ParticipationPercent of the races has no total. Higher wins.
public sealed class HighPointPercentage : ITotalRule
{
    public void Apply(ScoringWorkspace workspace, SeriesResults results, IEnumerable<Score> scores)
    {
        var percent = workspace.System.EffectiveParticipationPercent;
        results.IsPercentSystem = true;
        results.LowerScoreWins = false;
        results.PercentRequired = percent;
        var raceCount = results.Races.Count(r => (r.State ?? RaceState.Raced) == RaceState.Raced
            || r.State == RaceState.Preliminary);
        var requiredRaces = raceCount * ((percent ?? 0) / 100m);
        var host = workspace.Host;

        foreach (var comp in results.Competitors)
        {
            var compResults = results.Results[comp];
            var racesParticipated = compResults.CalculatedScores
                .Count(s => host.CountsAsStarted(s.Value.RawScore) || host.CountsAsParticipation(s.Value.RawScore));
            compResults.ParticipationPercent = racesParticipated * 100m / raceCount;

            compResults.TotalScore = null;

            // Leave out discards and codes that didn't come to the start.
            var racesToExclude = compResults.CalculatedScores
                .Where(s => s.Value.Discard
                    || (!String.IsNullOrEmpty(s.Value.RawScore.Code)
                        && !(host.GetScoreCode(s.Value.RawScore.Code).CameToStart ?? false)))
                .Select(s => s.Key.Id);
            var perfectScore = scores.Where(s => !racesToExclude.Contains(s.RaceId))
                .Count(host.CameToStart);
            var compTotal = compResults.CalculatedScores.Values
                .Sum(s => !s.Discard ? (s.ScoreValue ?? 0.0m) : 0.0m);

            compResults.PointsEarned = compTotal;
            compResults.PointsPossible = perfectScore;
            compResults.Average = perfectScore > 0 ? compTotal * 100 / perfectScore : 0;
            if (racesParticipated >= requiredRaces)
            {
                compResults.TotalScore = compResults.Average;
            }
        }
    }
}
