using System.Linq;

namespace SailScores.Core.Scoring.Adjustments;

// Wawasee 2026: a day's races count only when at least MinimumCompetitors boats in the
// series started a race that day. Records the races that don't count in
// workspace.RacesNotCounted, for the discard, average and total rules after it. No minimum:
// every race counts.
//
// Quirk: worked out again for each competitor, after earlier competitors' score code group
// overages have changed their codes, so the result can depend on the order competitors are
// scored in (docs/Scoring-Quirks.md). Kept here, in the adjustments, to keep that timing.
public sealed class RacesNeedStartersEachDay : IScoreAdjustment
{
    public void Apply(ScoringWorkspace workspace, SeriesResults results, SeriesCompetitorResults competitor)
    {
        var minimum = workspace.System.EffectiveSettings?.MinimumCompetitors;
        workspace.RacesNotCounted.Clear();
        if (minimum == null)
        {
            return;
        }
        foreach (var day in results.SailedRaces.GroupBy(r => r.Date?.Date))
        {
            var boatsStarted = day
                .SelectMany(r => workspace.RaceScoresForCounts(r, results))
                .Where(workspace.Host.CountsAsStarted)
                .Select(s => s.CompetitorId)
                .Distinct()
                .Count();
            if (boatsStarted < minimum.Value)
            {
                workspace.RacesNotCounted.UnionWith(day);
            }
        }
    }
}
