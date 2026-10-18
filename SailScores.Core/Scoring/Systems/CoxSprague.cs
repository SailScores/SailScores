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

// Cox-Sprague (https://www.hryra.org/wp-content/uploads/2015/06/Cox-Sprague-Scoring-System.pdf).
//   - Points come from the Cox-Sprague table, by place and number of starters.
//   - Coded results that came to the start score one place behind the last starter; codes
//     that didn't are fixed (usually 0). Series and average formulas aren't used, and
//     average-scored codes score nothing (issue 413).
//   - Discards follow the pattern over the races each boat sailed; each goes where it raises
//     the boat's percentage most.
//   - The total is the boat's points as a percentage of what winning every race it came to
//     the start in would have scored; boats short of the participation percent aren't ranked.
//   - High percentage wins; ties go to more races sailed, then A8.1, then A8.2.
public static class CoxSprague
{
    public static ScoringRecipe Create(ScoringSystem system) => new()
    {
        Name = "Cox-Sprague",
        RacePoints = new CoxSpraguePoints(),
        Formulas = FormulaSet.HighPoint,
        CodedResults = new CodedResultsOneBehindLast((place, starters) => CoxSpragueTable.GetScore(place, starters)),
        SeriesFormulas = false,
        Adjustments = [new RaiseToDefaultScore(), new AveragesScoreNothing()],
        Discards = [new CoxSpragueDiscards()],
        Total = new CoxSpragueTotals(),
        Eligibility = [new ParticipationPercent()],
        Ranking = Rankings.HighPoint,
        Glossary = new CoxSpragueGlossary()
    };
}
