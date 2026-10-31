using SailScores.Core.Model;
using SailScores.Core.Scoring.Adjustments;
using SailScores.Core.Scoring.Discards;
using SailScores.Core.Scoring.Eligibility;
using SailScores.Core.Scoring.Formulas;
using SailScores.Core.Scoring.RacePoints;
using SailScores.Core.Scoring.Tiebreaks;
using SailScores.Core.Scoring.Totals;

namespace SailScores.Core.Scoring.Systems;

// RRS Appendix A low point scoring, and its alternatives for first place.
//   - Points are places (A4); ties share the average of the places (A7).
//   - Codes score by their formula (A5): DNF and the like the boats in the series + 1,
//     penalties (ZFP, SCP) the place plus 20% of the DNF score, rounded to 0.1 (2025 rules).
//   - No score is worse than DNC.
//   - Discards follow the system's discard pattern, worst scores first.
//   - The total is the sum of the scores not discarded; boats short of the participation
//     percent aren't ranked.
//   - Low total wins; ties by A8.1, then A8.2.
public static class AppendixA
{
    public static ScoringRecipe Standard(ScoringSystem system) => Recipe("Appendix A", LowPointPlaces.AppendixA);

    // A4.2 alternatives: first scores 0, .7 or .75. With first 0, a score of 0 is a win: it
    // counts in averages, in A8.1 and in the total that ranks.
    public static ScoringRecipe FirstIsZero(ScoringSystem system) => new()
    {
        Name = "Appendix A, First = 0",
        RacePoints = LowPointPlaces.FirstIsZero,
        ZeroIsAScore = true,
        Adjustments = [new CapAtDefaultScore()],
        Discards = [new WorstScoresByPattern()],
        Total = new SumOfScores(),
        Eligibility = [new ParticipationPercent()],
        Ranking = Rankings.LowPointFirstIsZero
    };

    public static ScoringRecipe FirstIsPoint7(ScoringSystem system) =>
        Recipe("Appendix A, First = .7", LowPointPlaces.FirstIsPoint7);

    public static ScoringRecipe FirstIsPoint75(ScoringSystem system) =>
        Recipe("Appendix A, First = .75", LowPointPlaces.FirstIsPoint75);

    // Before the 2025 rules: penalties round to a whole number. No participation requirement
    // (a quirk: docs/Scoring-Quirks.md).
    public static ScoringRecipe Pre2025(ScoringSystem system) => new()
    {
        Name = "Appendix A (pre-2025)",
        RacePoints = LowPointPlaces.AppendixA,
        Formulas = FormulaSet.Standard.With(new PlacePlusPercentFormula(PlacePlusPercentFormula.RoundingPre2025)),
        Adjustments = [new CapAtDefaultScore()],
        Discards = [new WorstScoresByPattern()],
        Total = new SumOfScores(),
        Eligibility = [],
        Ranking = Rankings.LowPoint
    };

    private static ScoringRecipe Recipe(string name, LowPointPlaces racePoints) => new()
    {
        Name = name,
        RacePoints = racePoints,
        Adjustments = [new CapAtDefaultScore()],
        Discards = [new WorstScoresByPattern()],
        Total = new SumOfScores(),
        Eligibility = [new ParticipationPercent()],
        Ranking = Rankings.LowPoint
    };
}
