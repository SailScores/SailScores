using SailScores.Core.Model;
using SailScores.Core.Scoring.Adjustments;
using SailScores.Core.Scoring.Discards;
using SailScores.Core.Scoring.Eligibility;
using SailScores.Core.Scoring.Formulas;
using SailScores.Core.Scoring.Glossary;
using SailScores.Core.Scoring.RacePoints;
using SailScores.Core.Scoring.Tiebreaks;
using SailScores.Core.Scoring.Totals;

namespace SailScores.Core.Scoring.Systems;

// High point percentage.
//   - A boat scores a point for each boat that came to the start and finished behind it.
//   - Penalties (PLC%) take a percentage of the boats at the start off the points.
//   - No score is worse than DNC.
//   - Discards follow the pattern over the races each boat sailed, lowest share of the
//     perfect score first.
//   - The total is the points earned as a percentage of the points possible in the races the
//     boat sailed and didn't discard; boats short of the participation percent aren't ranked.
//   - High percentage wins; ties go to more races sailed, then A8.1, then A8.2.
public static class HighPointPercentages
{
    public static ScoringRecipe Create(ScoringSystem system) => new()
    {
        Name = "High Point Percentage",
        RacePoints = new HighPointPercentagePoints(),
        Formulas = FormulaSet.HighPoint,
        Adjustments = [new RaiseToDefaultScore()],
        Discards = [new WorstPercentagesByPattern(lowerScoreWins: false, countAveragedRaces: false)],
        Total = new HighPointPercentage(),
        Eligibility = [new ParticipationPercent()],
        Ranking = Rankings.HighPoint,
        Glossary = new NotAtStartExcludedGlossary()
    };
}
