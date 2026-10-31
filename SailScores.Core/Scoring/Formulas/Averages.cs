using System;
using System.Collections.Generic;
using System.Linq;

namespace SailScores.Core.Scoring.Formulas;

// What the average formulas share.
internal static class Averages
{
    public static bool IsAverage(IFormulaHost host, CalculatedScore score) =>
        host.FormulaFor(score.RawScore.Code)?.Step == FormulaStep.Average;

    // The scores an average is taken over: not other average-scored races or scores without a
    // value, and not scores of 0 unless the recipe counts them (First = 0, where a win scores
    // 0; elsewhere 0 marks a score left out of averages).
    public static IEnumerable<CalculatedScore> Averaged(IFormulaHost host, IEnumerable<CalculatedScore> scores) =>
        scores.Where(s => (host.AveragesCountZeroScores ? s.ScoreValue.HasValue : (s.ScoreValue ?? 0m) != 0m)
            && !IsAverage(host, s));

    public static decimal Round(decimal average, int decimals) =>
        Math.Round(average, decimals, MidpointRounding.AwayFromZero);
}
