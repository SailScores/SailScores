using SailScores.Core.Model;

namespace SailScores.Core.Scoring.Formulas;

// How a score code is scored: the Formula field of a ScoreCode ("FIX", "AVE ND", "PLC%"...).
// One class per formula. A calculator finds them in its FormulaSet.
public interface IScoreCodeFormula
{
    // The Formula value stored on score codes, in upper case.
    string Name { get; }

    // When in the scoring of a competitor the formula is evaluated.
    FormulaStep Step { get; }

    // The score for context.Score, or null for no score. Formulas with Step None are never
    // evaluated.
    decimal? Evaluate(FormulaContext context);

    // The code glossary text shown with results, e.g. "Fixed at 3".
    string Describe(ScoreCode code);
}

public enum FormulaStep
{
    // Not evaluated (TIE is handled by each calculator's place scoring; COD is unfinished).
    None,
    // From the race alone: fixed and manual values, boat counts in the race, penalties.
    Race,
    // From the series: boats in the series. Evaluated after race formulas.
    Series,
    // From the competitor's other scores. Evaluated last, so the scores averaged are final.
    Average
}
