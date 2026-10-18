using SailScores.Api.Enumerations;
using System.Linq;

namespace SailScores.Core.Scoring.Eligibility;

// A competitor must have sailed (started, or a code counting as participation) at least the
// system's ParticipationPercent of the races sailed, discards included, to be ranked. Sets
// each competitor's ParticipationPercent and the results' PercentRequired. No percent, or no
// races: everyone is eligible and nothing is set.
public sealed class ParticipationPercent : IEligibilityRule
{
    public void Apply(ScoringWorkspace workspace, SeriesResults results)
    {
        var percent = workspace.System?.EffectiveParticipationPercent;
        if (!(percent > 0))
        {
            return;
        }

        results.PercentRequired = percent;

        var totalRaceCount = results.Races.Count(r =>
            (r.State ?? RaceState.Raced) == RaceState.Raced
            || r.State == RaceState.Preliminary);
        if (totalRaceCount == 0)
        {
            return;
        }

        var requiredRaces = totalRaceCount * ((percent ?? 0m) / 100m);
        var host = workspace.Host;
        foreach (var comp in results.Competitors)
        {
            var compResults = results.Results[comp];
            var racesParticipated = compResults.CalculatedScores
                .Count(s => host.CountsAsStarted(s.Value.RawScore) || host.CountsAsParticipation(s.Value.RawScore));

            compResults.ParticipationPercent = racesParticipated * 100m / totalRaceCount;

            if (racesParticipated < requiredRaces)
            {
                compResults.TotalScore = null;
            }
        }
    }
}
