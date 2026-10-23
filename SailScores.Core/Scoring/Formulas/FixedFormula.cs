using System;
using SailScores.Core.Model;

namespace SailScores.Core.Scoring.Formulas;

// FIX: the code's Formula Value.
public sealed class FixedFormula : IScoreCodeFormula
{
    public const string FormulaName = "FIX";

    public string Name => FormulaName;
    public FormulaStep Step => FormulaStep.Race;

    public decimal? Evaluate(FormulaContext context)
    {
        if (!string.Equals(context.Code.Formula, FormulaName, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }
        return context.Code.FormulaValue;
    }

    public string Describe(ScoreCode code) => $"Fixed at {code.FormulaValue?.ToString("0.###")}";
}
