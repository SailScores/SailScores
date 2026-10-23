using SailScores.Api.Enumerations;
using System.Linq;

namespace SailScores.Core.Scoring.Eligibility;

// A competitor must have sailed (started, or a code counting as participation) at least the
// system's ParticipationPercent of the races sailed, discards included, to be ranked. Sets
// each competitor's ParticipationPercent and the results' PercentRequired. No percent, or no
// races: everyone is eligible and nothing is set.
// After discards (PBG): the races the competitor sailed and didn't discard, over the races
// sailed less the maximum discards: what a boat sailing every race would discard. Child series
// discards: the discard pattern for each child series' races, summed.
//   3 child series of 8 races, 1 discard each; a boat sails 16 and discards 2: 14 of 21.
public sealed class ParticipationPercent : IEligibilityRule
{
    private readonly bool _afterDiscards;

    public ParticipationPercent(bool afterDiscards = false)
    {
        _afterDiscards = afterDiscards;
    }

    public void Apply(ScoringWorkspace workspace, SeriesResults results)
    {
        var percent = workspace.System?.EffectiveParticipationPercent;
        if (!(percent > 0))
        {
            return;
        }

        results.PercentRequired = percent;

        var totalRaceCount = SailedRaceCount(results);
        if (totalRaceCount == 0)
        {
            return;
        }

        var host = workspace.Host;
        var raceCount = RacesToParticipateIn(workspace, results, totalRaceCount, _afterDiscards);
        foreach (var comp in results.Competitors)
        {
            var compResults = results.Results[comp];
            var racesParticipated = Participated(host, compResults, _afterDiscards);

            compResults.ParticipationPercent = raceCount == 0 ? 0m : racesParticipated * 100m / raceCount;

            if (racesParticipated < raceCount * ((percent ?? 0m) / 100m))
            {
                compResults.TotalScore = null;
            }
        }
    }

    public static int SailedRaceCount(SeriesResults results) =>
        results.Races.Count(r =>
            (r.State ?? RaceState.Raced) == RaceState.Raced
            || r.State == RaceState.Preliminary);

    // The races competitors are measured against: after discards, less the maximum discards.
    public static int RacesToParticipateIn(
        ScoringWorkspace workspace,
        SeriesResults results,
        int totalRaceCount,
        bool afterDiscards) =>
        afterDiscards
            ? totalRaceCount - MaximumDiscards(workspace, results)
            : totalRaceCount;

    // The discards the pattern gives a boat that sails every race, per discard group. Child
    // series discards group by child series.
    private static int MaximumDiscards(ScoringWorkspace workspace, SeriesResults results)
    {
        var method = workspace.DiscardMethod == DiscardMethod.PreserveChildSeriesDiscards
            ? DiscardMethod.PerChildSeries
            : workspace.DiscardMethod;
        return DiscardRaceGroups.Get(method, results.SailedRaces)
            .Sum(g => workspace.Host.DiscardsForRaceCount(g.Count));
    }

    // The races a competitor sailed; after discards, leaving out their discarded races.
    public static int Participated(IScoringHost host, SeriesCompetitorResults compResults, bool afterDiscards) =>
        compResults.CalculatedScores.Values
            .Where(s => !afterDiscards || !s.Discard)
            .Count(s => host.CountsAsStarted(s.RawScore) || host.CountsAsParticipation(s.RawScore));
}
