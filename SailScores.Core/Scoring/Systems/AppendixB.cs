using SailScores.Core.Model;
using SailScores.Core.Scoring.Adjustments;
using SailScores.Core.Scoring.Discards;
using SailScores.Core.Scoring.Eligibility;
using SailScores.Core.Scoring.RacePoints;
using SailScores.Core.Scoring.Tiebreaks;
using SailScores.Core.Scoring.Totals;

namespace SailScores.Core.Scoring.Systems;

// RRS 2025-2028 Appendix B (windsurfing fleet racing): Appendix A as changed by B8.
// Rules, clause map and worked examples: docs/ScoringSystems/AppendixB-Windsurfing.md.
//   - Points, code formulas and the DNC cap are as Appendix A. The base system's codes
//     score by A5.2 and have no ZFP (rule 30.2 is deleted).
//   - Discards follow the discard pattern, worst scores first, earlier races first among
//     equal scores. The base system's pattern excludes one score from 5 races and two
//     from 12 (B8 A2.1).
//   - Low total wins; ties by the excluded scores, best first (B8 A8.1), then all scores
//     best first (B8 A8.2), then the last race (B8 A8.3).
public static class AppendixB
{
    public static ScoringRecipe Windsurfing(ScoringSystem system) => new()
    {
        Name = "Appendix B Windsurfing",
        RacePoints = LowPointPlaces.AppendixA,
        Adjustments = [new CapAtDefaultScore()],
        Discards = [new WorstScoresByPattern()],
        Total = new SumOfScores(),
        Eligibility = [new ParticipationPercent()],
        Ranking = Ranking.By(new LowestTotal(zeroTotalRanksLast: true))
            .Then(new AppendixBExcludedScores())
            .Then(new PwaBetterScoresIncludingDiscards())
            .Then(AppendixA82.LowPoint)
    };
}
