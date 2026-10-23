using System.Collections.Generic;
using System.Linq;
using SailScores.Core.Model;
using SailScores.Core.Scoring.Eligibility;

namespace SailScores.Core.Scoring.Totals;

// Low point averages: the total is the average of the scores not discarded, leaving out
// scores of 0 (ZeroWhenNotAveraged sets those). A competitor who sailed (started, or a code
// counting as participation) fewer than ParticipationPercent of the races has no total;
// after discards (PBG), see Eligibility/ParticipationPercent.
// Shown as a percentage system: results list points earned over races counted.
public sealed class LowPointAverage : ITotalRule
{
    private readonly bool _participationAfterDiscards;

    public LowPointAverage(bool participationAfterDiscards = false)
    {
        _participationAfterDiscards = participationAfterDiscards;
    }

    public void Apply(ScoringWorkspace workspace, SeriesResults results, IEnumerable<Score> scores)
    {
        var percent = workspace.System.EffectiveParticipationPercent;
        results.IsPercentSystem = true;
        results.LowerScoreWins = true;
        results.PercentRequired = percent ?? 0m;

        var totalRaceCount = ParticipationPercent.SailedRaceCount(results);
        var participationRaces = ParticipationPercent.RacesToParticipateIn(
            workspace, results, totalRaceCount, _participationAfterDiscards);
        var host = workspace.Host;

        foreach (var comp in results.Competitors)
        {
            var compResults = results.Results[comp];
            var racesParticipated = ParticipationPercent.Participated(
                host, compResults, _participationAfterDiscards);
            compResults.ParticipationPercent = racesParticipated * 100.0m / participationRaces;
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

                if (racesParticipated >= participationRaces * ((percent ?? 0) / 100m))
                {
                    compResults.TotalScore = compResults.Average;
                }
            }
        }
    }
}
