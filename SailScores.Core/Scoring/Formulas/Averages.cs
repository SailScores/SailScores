using System;
using System.Collections.Generic;
using System.Linq;

namespace SailScores.Core.Scoring.Formulas;

// What the average formulas share.
internal static class Averages
{
    public static bool IsAverage(IFormulaHost host, CalculatedScore score) =>
        host.FormulaFor(score.RawScore.Code)?.Step == FormulaStep.Average;

    // The scores an average is taken over. Quirk: scores of 0 are left out, as well as other
    // average-scored races, so under First = 0 a win doesn't count (docs/Scoring-Quirks.md).
    public static IEnumerable<CalculatedScore> Averaged(IFormulaHost host, IEnumerable<CalculatedScore> scores) =>
        scores.Where(s => (s.ScoreValue ?? 0m) != 0m && !IsAverage(host, s));

    public static decimal Round(decimal average, int decimals) =>
        Math.Round(average, decimals, MidpointRounding.AwayFromZero);
}
