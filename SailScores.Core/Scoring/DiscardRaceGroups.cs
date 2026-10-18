using System.Collections.Generic;
using System.Linq;
using SailScores.Api.Enumerations;
using SailScores.Core.Model;

namespace SailScores.Core.Scoring
{
    // Splits a series' sailed races into the groups that discards are chosen from.
    // The discard pattern is applied to each group on its own.
    // PreserveChildSeriesDiscards doesn't use groups; calculators handle it directly.
    public static class DiscardRaceGroups
    {
        public static IReadOnlyList<IReadOnlyCollection<Race>> Get(
            DiscardMethod method,
            IEnumerable<Race> sailedRaces)
        {
            var races = sailedRaces?.ToList() ?? new List<Race>();
            switch (method)
            {
                case DiscardMethod.PerDay:
                    return Group(races, r => r.Date?.Date);
                case DiscardMethod.PerChildSeries:
                    return Group(races, r => r.SourceChildSeriesId);
                default:
                    // Kept as-is (no de-duplication) so race counts match earlier versions.
                    return new[] { races };
            }
        }

        // A race in more than one child series counts once, in the first group it appears in,
        // the same way a competitor's scores are keyed by race.
        private static IReadOnlyList<IReadOnlyCollection<Race>> Group<TKey>(
            IEnumerable<Race> races,
            System.Func<Race, TKey> keySelector)
        {
            return races
                .Distinct()
                .GroupBy(keySelector)
                .Select(g => (IReadOnlyCollection<Race>)g.ToList())
                .ToList();
        }
    }
}
