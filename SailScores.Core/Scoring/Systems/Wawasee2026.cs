using SailScores.Core.Model;
using SailScores.Core.Scoring.Adjustments;
using SailScores.Core.Scoring.Discards;
using SailScores.Core.Scoring.RacePoints;
using SailScores.Core.Scoring.Tiebreaks;
using SailScores.Core.Scoring.Totals;

namespace SailScores.Core.Scoring.Systems;

// Wawasee 2026 club series ("Low Point Average Scoring System" in their instructions).
//   - Race points are finishing places (Appendix A).
//   - A boat with no result, or a code the system doesn't have, is DNP (did not participate).
//   - Average-scored codes (DNP, DNS, excused absence, breakdown: AVE formulas) score the boat's
//     average finish: the mean of its other scores in races that count, not rounded.
//   - Series points are the total of race points plus penalties: a boat that sailed (started
//     or a participation code such as excused) fewer than PenaltyThresholdPercent of the
//     races that count gets PenaltyScoreValue for each race short. The races needed round to
//     the nearest race, a half up, as the club's spreadsheet does: 70% of 16 is 11.2, so 11.
//   - Ranking needs ParticipationPercent of the races that count competed in: came to the
//     start (a DNS, scored like DNP, and excused absences don't count; a breakdown does).
//   - A race counts only when at least MinimumCompetitors boats started a race that day;
//     otherwise every score in it is a discard. There are no other discards.
//   - Low total wins; ties go to the lower average finish, then RRS A8.
// The club's rules: docs/ScoringSystems/Wawasee2026.md.
public static class Wawasee2026
{
    public static ScoringRecipe Create(ScoringSystem system) => new()
    {
        Name = "Wawasee 2026",
        DefaultCode = "DNP",
        RacePoints = LowPointPlaces.AppendixA,
        Adjustments =
        [
            new CapAtDefaultScore(),
            new RacesNeedStartersEachDay(),
            new AverageFinishOverCountedRaces()
        ],
        Discards = [new UncountedRacesOnly()],
        // Includes the participation requirement.
        Total = new Wawasee2026Totals(),
        Eligibility = [],
        Ranking = Ranking.By(new LowestTotal(zeroTotalRanksLast: false))
            .Then(new LowerAverage())
            .Then(new AppendixA81LowPoint())
            .Then(AppendixA82.LowPoint)
    };
}
