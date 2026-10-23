using SailScores.Core.Model;

namespace SailScores.Core.Scoring.Formulas;

// A count of boats plus the code's Formula Value: FIN+, CTS+, STR+ and SER+.
public abstract class CompetitorCountFormula : IScoreCodeFormula
{
    public abstract string Name { get; }
    public abstract FormulaStep Step { get; }

    // What is counted, for the glossary: "Number of boats that finished race".
    protected abstract string CountDescription { get; }

    protected abstract int Count(FormulaContext context);

    public decimal? Evaluate(FormulaContext context) =>
        Count(context) + (context.Code.FormulaValue ?? 0m);

    public string Describe(ScoreCode code) =>
        (code.FormulaValue ?? 0m) == 0m
            ? CountDescription
            : $"{CountDescription} + {code.FormulaValue?.ToString("0.###")}";
}
