using System.Linq;
using SailScores.Core.Scoring.RacePoints;

namespace SailScores.Core.Scoring.Discards;

// VARC 2026: a race counts only when at least MinimumCompetitors boats in the series were
// scored: a result or a code that came to the start, not DNC (VarcEventPoints.IsRacing).
// Scores in other races are discards, and the races are recorded in
// workspace.RacesNotCounted (the A8.2 tiebreak skips them). No minimum: every race counts.
//
// Quirk: worked out again for each competitor, after earlier competitors' score code group
// overages have changed their codes (docs/Scoring-Quirks.md).
public sealed class RacesNeedBoatsAtStart : IDiscardRule
{
    public void Apply(ScoringWorkspace workspace, SeriesResults results, SeriesCompetitorResults competitor)
    {
        var minimum = workspace.System.EffectiveSettings?.MinimumCompetitors;
        foreach (var race in results.SailedRaces)
        {
            var counts = minimum == null
                || workspace.RaceScoresForCounts(race, results).Count(s => VarcEventPoints.IsRacing(workspace.Host, s)) >= minimum.Value;
            if (counts)
            {
                workspace.RacesNotCounted.Remove(race);
            }
            else
            {
                workspace.RacesNotCounted.Add(race);
                competitor.CalculatedScores[race].Discard = true;
            }
        }
    }
}
