using System;
using System.Collections.Generic;
using System.Linq;
using SailScores.Api.Enumerations;
using SailScores.Core.Model;

namespace SailScores.Core.Scoring.Discards;

// What the discard pattern rules share: the discard method's groups of races (the whole
// series, each day, or each child series), or the child series' own discards in a summary
// series that keeps them.
internal static class PatternDiscards
{
    // countFor gets a group's races and the competitor's scores in them. orderCandidates
    // returns the discardable scores, first to discard first.
    public static void Mark(
        ScoringWorkspace workspace,
        SeriesResults results,
        SeriesCompetitorResults competitor,
        Func<IReadOnlyCollection<Race>, IList<CalculatedScore>, int> countFor,
        Func<IEnumerable<CalculatedScore>, IEnumerable<CalculatedScore>> orderCandidates)
    {
        if (workspace.DiscardMethod == DiscardMethod.PreserveChildSeriesDiscards)
        {
            MarkChildSeriesDiscards(results, competitor);
            return;
        }

        foreach (var groupRaces in DiscardRaceGroups.Get(workspace.DiscardMethod, results.SailedRaces))
        {
            var raceSet = new HashSet<Race>(groupRaces);
            var groupScores = competitor.CalculatedScores
                .Where(kv => raceSet.Contains(kv.Key))
                .Select(kv => kv.Value)
                .ToList();
            var count = countFor(groupRaces, groupScores);
            foreach (var score in orderCandidates(groupScores).Take(count))
            {
                score.Discard = true;
            }
        }
    }

    // Whatever the child series discarded, from the races' CompetitorIdsDiscardedInChild (set
    // when a summary series loads its children's races).
    private static void MarkChildSeriesDiscards(SeriesResults results, SeriesCompetitorResults competitor)
    {
        var competitorId = competitor.Competitor?.Id ?? Guid.Empty;
        foreach (var race in results.SailedRaces)
        {
            if (race.CompetitorIdsDiscardedInChild?.Contains(competitorId) == true
                && competitor.CalculatedScores.TryGetValue(race, out var score))
            {
                score.Discard = true;
            }
        }
    }
}
