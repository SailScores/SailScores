using System.Linq;

namespace SailScores.Core.Scoring.Discards;

// Wawasee 2026: the only discards are scores in races that don't count
// (workspace.RacesNotCounted, from RacesNeedStartersEachDay).
public sealed class UncountedRacesOnly : IDiscardRule
{
    public void Apply(ScoringWorkspace workspace, SeriesResults results, SeriesCompetitorResults competitor)
    {
        foreach (var race in results.SailedRaces.Where(workspace.RacesNotCounted.Contains))
        {
            competitor.CalculatedScores[race].Discard = true;
        }
    }
}
