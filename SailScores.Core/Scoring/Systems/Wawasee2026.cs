using SailScores.Core.Model;
using SailScores.Core.Scoring.Adjustments;
using SailScores.Core.Scoring.Discards;
using SailScores.Core.Scoring.RacePoints;
using SailScores.Core.Scoring.Tiebreaks;
using SailScores.Core.Scoring.Totals;

namespace SailScores.Core.Scoring.Systems;

// Wawasee 2026 club series ("Low Point Average Scoring System" in their instructions).
//   - Race points are finishing places (Appendix A).
//   - Average-scored codes (DNC, excused absence, breakdown: AVE formulas) score the boat's
//     average finish: the mean of its other scores in races that count.
//   - Series points are the total of race points plus penalties: a boat that sailed (started
//     or a participation code such as excused) fewer than PenaltyThresholdPercent of the
//     races that count gets PenaltyScoreValue for each race short. The races needed round up:
//     their rule rounds to the nearest race, then adds a penalty if that falls below 70%.
//   - Ranking needs ParticipationPercent of the races that count actually started (excused
//     absences don't count; a breakdown, which starts, does).
//   - A race counts only when at least MinimumCompetitors boats started a race that day;
//     otherwise every score in it is a discard. There are no other discards.
//   - Low total wins; ties go to the lower average finish, then RRS A8.
public static class Wawasee2026
{
    public static ScoringRecipe Create(ScoringSystem system) => new()
    {
        Name = "Wawasee 2026",
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
