using System;
using System.Collections.Generic;
using System.Linq;
using SailScores.Api.Enumerations;
using SailScores.Core.Model;

namespace SailScores.Core.Scoring.Totals;

// Cox-Sprague: the total is the competitor's points in races not discarded, as a percentage
// of what winning each of those races would have scored (codes that didn't come to the start
// left out). A competitor who sailed fewer than ParticipationPercent of the races has no
// total. Higher wins.
public sealed class CoxSpragueTotals : ITotalRule
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

        var starterCounts = new Dictionary<Guid, int>();
        foreach (Race r in results.SailedRaces)
        {
            var relevantScores = workspace.ScoresIn(r).Where(s => results.Competitors.Any(c => c.Id == s.CompetitorId));
            starterCounts[r.Id] = relevantScores.Count(host.CountsAsStarted);
        }

        foreach (var comp in results.Competitors)
        {
            var compResults = results.Results[comp];
            var racesParticipated = compResults.CalculatedScores
                .Count(s => host.CountsAsStarted(s.Value.RawScore) || host.CountsAsParticipation(s.Value.RawScore));
            compResults.ParticipationPercent = racesParticipated * 100.0m / raceCount;

            // Leave out discards and codes that didn't come to the start.
            var racesToExclude = compResults.CalculatedScores
                .Where(s => s.Value.Discard
                    || (!String.IsNullOrEmpty(s.Value.RawScore.Code)
                        && !(host.GetScoreCode(s.Value.RawScore.Code).CameToStart ?? false)))
                .Select(s => s.Key.Id);

            int perfectScore = 0;
            foreach (Race r in results.SailedRaces)
            {
                if (!racesToExclude.Contains(r.Id))
                {
                    perfectScore += CoxSpragueTable.GetScore(1, starterCounts[r.Id]);
                }
            }

            var compTotal = compResults.CalculatedScores.Values
                .Sum(s => !s.Discard ? (s.ScoreValue ?? 0.0m) : 0.0m);
            compResults.TotalScore = null;
            compResults.PointsEarned = compTotal;
            compResults.PointsPossible = perfectScore;
            compResults.Average = perfectScore == 0 ? 0 : compTotal * 100 / perfectScore;

            if (racesParticipated >= requiredRaces)
            {
                compResults.TotalScore = compResults.Average;
            }
        }
    }
}
