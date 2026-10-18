using System;
using SailScores.Core.Model;

namespace SailScores.Core.Scoring.Formulas;

// MAN: the points the scorer entered for the boat, or its place if none were entered.
public sealed class ManualFormula : IScoreCodeFormula
{
    public const string FormulaName = "MAN";

    public string Name => FormulaName;
    public FormulaStep Step => FormulaStep.Race;

    public decimal? Evaluate(FormulaContext context)
    {
        // Quirk: found ignoring case, but only scored when written in upper case
        // (docs/Scoring-Quirks.md).
        if (context.Code.Formula != FormulaName)
        {
            return null;
        }
        var score = context.Score.RawScore;
        if (score.CodePoints.HasValue)
        {
            return score.CodePoints;
        }
        if (score.Place.HasValue)
        {
            return Convert.ToDecimal(score.Place);
        }
        return null;
    }

    public string Describe(ScoreCode code) => "Manually entered value";
}
