using System.Linq;

namespace SailScores.Core.Scoring.Eligibility;

// PBG: participation leaves out discarded races. A competitor's percent is the races they
// sailed and didn't discard, out of the races sailed less their discards; they're ranked
// (total = average) when it reaches the system's ParticipationPercent. Replaces whatever an
// earlier rule set.
public sealed class ParticipationOfRacesNotDiscarded : IEligibilityRule
{
    public void Apply(ScoringWorkspace workspace, SeriesResults results)
    {
        var percentRequired = workspace.System.EffectiveParticipationPercent ?? 0m;
        var sailedRaceCount = results.GetSailedRaceCount();
        var host = workspace.Host;
        foreach (var compResults in results.Results.Values)
        {
            var keptScores = compResults.CalculatedScores.Values.Where(s => !s.Discard).ToList();
            var racesCounted = sailedRaceCount - (compResults.CalculatedScores.Count - keptScores.Count);
            var racesSailed = keptScores.Count(s =>
                host.CountsAsStarted(s.RawScore) || host.CountsAsParticipation(s.RawScore));

            compResults.ParticipationPercent = racesCounted > 0
                ? racesSailed * 100m / racesCounted
                : 0m;
            compResults.TotalScore = compResults.Average != null
                && compResults.ParticipationPercent >= percentRequired
                    ? compResults.Average
                    : null;
        }
    }
}
