using System;
using System.Collections.Generic;
using System.Linq;
using SailScores.Core.Model;
using SailScores.Core.Scoring.Adjustments;

namespace SailScores.Core.Scoring.Totals;

// Wawasee 2026: points are the sum of scores in races that count, plus PenaltyScoreValue for
// each race short of PenaltyThresholdPercent of those races sailed (started or a
// participation code; races needed round to the nearest race, a half up). A competitor is ranked only with
// ParticipationPercent of the counted races competed in (came to the start; excused
// absences don't count) and an average finish. Sets the
// penalties column and the results' number of discards to the most any competitor has.
public sealed class Wawasee2026Totals : ITotalRule
{
    public void Apply(ScoringWorkspace workspace, SeriesResults results, IEnumerable<Score> scores)
    {
        var system = workspace.System;
        results.IsPercentSystem = true;
        results.LowerScoreWins = true;
        results.PercentRequired = system.EffectiveParticipationPercent;

        var countedRaces = results.SailedRaces.Where(r => !workspace.RacesNotCounted.Contains(r)).ToList();
        var settings = system.EffectiveSettings;
        var penaltyValue = settings?.PenaltyScoreValue;
        var racesToAvoidPenalty = settings?.PenaltyThresholdPercent == null
            ? 0
            : (int)Math.Round(countedRaces.Count * settings.PenaltyThresholdPercent.Value / 100m,
                MidpointRounding.AwayFromZero);
        var percentRequired = system.EffectiveParticipationPercent ?? 0m;
        var host = workspace.Host;

        foreach (var compResults in results.Results.Values)
        {
            var countedScores = countedRaces.Select(r => compResults.CalculatedScores[r]).ToList();
            var racesCompeted = countedScores.Count(s =>
                host.CountsAsStarted(s.RawScore) || host.CameToStart(s.RawScore));
            var racesSailed = countedScores.Count(s =>
                host.CountsAsStarted(s.RawScore) || host.CountsAsParticipation(s.RawScore));

            var penaltyCount = penaltyValue == null ? 0 : Math.Max(0, racesToAvoidPenalty - racesSailed);
            var penaltyPoints = penaltyCount * (penaltyValue ?? 0m);
            if (penaltyCount > 0)
            {
                compResults.ColumnValues[ScoringColumns.PenaltiesKey] = new ScoringColumnValue
                {
                    Value = penaltyCount,
                    Multiplier = penaltyValue
                };
            }

            var points = countedScores.Sum(s => s.ScoreValue ?? 0m) + penaltyPoints;
            compResults.Average = AverageFinishOverCountedRaces.For(workspace, compResults);
            compResults.PointsEarned = points;
            compResults.PointsPossible = countedRaces.Count;
            compResults.TotalScoreIncludingDiscards =
                compResults.CalculatedScores.Values.Sum(s => s.ScoreValue ?? 0m) + penaltyPoints;
            compResults.ParticipationPercent = countedRaces.Count > 0
                ? racesCompeted * 100m / countedRaces.Count
                : 0m;
            compResults.TotalScore = compResults.Average != null
                && compResults.ParticipationPercent >= percentRequired
                    ? points
                    : null;
        }

        results.NumberOfDiscards = results.Results.Values
            .Select(r => r.CalculatedScores.Values.Count(s => s.Discard))
            .DefaultIfEmpty(0)
            .Max();
    }
}
