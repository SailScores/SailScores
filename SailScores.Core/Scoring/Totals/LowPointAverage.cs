using System.Collections.Generic;
using System.Linq;
using SailScores.Api.Enumerations;
using SailScores.Core.Model;

namespace SailScores.Core.Scoring.Totals;

// Low point averages: the total is the average of the scores not discarded, leaving out
// scores of 0 (ZeroWhenNotAveraged sets those). A competitor who sailed (started, or a code
// counting as participation) fewer than ParticipationPercent of the races has no total.
// Shown as a percentage system: results list points earned over races counted.
public sealed class LowPointAverage : ITotalRule
{
    public void Apply(ScoringWorkspace workspace, SeriesResults results, IEnumerable<Score> scores)
    {
        var percent = workspace.System.EffectiveParticipationPercent;
        results.IsPercentSystem = true;
        results.LowerScoreWins = true;
        results.PercentRequired = percent ?? 0m;

        var totalRaceCount = results.Races.Count(r =>
            (r.State ?? RaceState.Raced) == RaceState.Raced
            || r.State == RaceState.Preliminary);
        var requiredRaces = totalRaceCount * ((percent ?? 0) / 100m);
        var host = workspace.Host;

        foreach (var comp in results.Competitors)
        {
            var compResults = results.Results[comp];
            var racesParticipated = compResults.CalculatedScores.Values
                .Count(s => host.CountsAsStarted(s.RawScore) || host.CountsAsParticipation(s.RawScore));
            compResults.ParticipationPercent = racesParticipated * 100.0m / totalRaceCount;
            var raceCount = compResults.CalculatedScores.Values
                .Count(s => !s.Discard && (s.ScoreValue ?? 0.0m) != 0.0m);

            compResults.TotalScore = null;
            if (raceCount != 0)
            {
                compResults.PointsEarned = compResults.CalculatedScores.Values
                    .Where(s => !s.Discard)
                    .Sum(s => s.ScoreValue ?? 0.0m);
                compResults.Average = compResults.PointsEarned / raceCount;
                compResults.PointsPossible = raceCount;

                if (racesParticipated >= requiredRaces)
                {
                    compResults.TotalScore = compResults.Average;
                }
            }
        }
    }
}
