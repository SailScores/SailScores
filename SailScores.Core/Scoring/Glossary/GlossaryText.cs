using System;
using System.Text;
using SailScores.Core.Model;
using SailScores.Core.Scoring.Formulas;

namespace SailScores.Core.Scoring.Glossary;

// Text the glossaries share.
public static class GlossaryText
{
    // The code's formula as its formula class describes it, plus "; not excludable" when
    // the code can't be discarded.
    public static string Formula(FormulaSet formulas, ScoreCode code)
    {
        var text = new StringBuilder(formulas.Find(code)?.Describe(code) ?? String.Empty);
        if (!(code.Discardable ?? true))
        {
            text.Append("; not excludable");
        }
        return text.ToString();
    }

    // For a code a system leaves out of the series calculation entirely.
    public static string Excluded(ScoreCode code) =>
        CountsAsParticipation(code) ? "Excluded; counts toward participation" : "Excluded";

    public static bool CameToStart(ScoreCode code) => code.CameToStart ?? false;

    public static bool CountsAsParticipation(ScoreCode code) =>
        code != null && ((code.CameToStart ?? false) || (code.CountAsParticipation ?? false));

    // Cox-Sprague and Top X: a code fixed at 0 is scored one behind the last starter.
    public static string FixedAtZeroAsOneBehindLast(string formula) =>
        formula.Contains("Fixed") && formula.Contains("0") ? "One greater than # came to start." : formula;
}
