using System;
using SailScores.Core.Model;

namespace SailScores.Core.Scoring.Formulas;

// TIE: tied with the boat finishing just ahead. Each calculator's place scoring averages the
// tied places, so the formula itself isn't evaluated.
public sealed class TieFormula : IScoreCodeFormula
{
    public const string FormulaName = "TIE";

    public string Name => FormulaName;
    public FormulaStep Step => FormulaStep.None;

    public decimal? Evaluate(FormulaContext context) => null;

    public string Describe(ScoreCode code) => "Average of tied places";

    public static bool IsTie(ScoreCode code) =>
        string.Equals(code?.Formula, FormulaName, StringComparison.OrdinalIgnoreCase);
}
