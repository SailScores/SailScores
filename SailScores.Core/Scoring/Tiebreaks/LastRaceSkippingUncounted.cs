using System.Collections.Generic;
using System.Linq;

namespace SailScores.Core.Scoring.Tiebreaks;

// RRS A8.2 as VARC 2026 applies it, high point: compare scores in the last race, then the
// race before, and so on, discards included, skipping races that didn't count. A score with
// no value counts as 0. Races that don't count are workspace.RacesNotCounted.
public sealed class LastRaceSkippingUncounted : ITieBreak
{
    public int Compare(ScoringWorkspace workspace, SeriesCompetitorResults x, SeriesCompetitorResults y) =>
        HighPointSequence.Compare(Scores(workspace, x), Scores(workspace, y));

    private static List<decimal> Scores(ScoringWorkspace workspace, SeriesCompetitorResults results) =>
        results.CalculatedScores
            .Where(s => !workspace.RacesNotCounted.Contains(s.Key))
            .OrderByDescending(s => s.Key.Date)
            .ThenByDescending(s => s.Key.Order)
            .Select(s => s.Value.ScoreValue ?? 0m)
            .ToList();
}
