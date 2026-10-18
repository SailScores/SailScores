using System.Linq;

namespace SailScores.Core.Scoring.Formulas;

// FIN+: boats that finished the race, plus the Formula Value.
public sealed class FinishersPlusFormula : CompetitorCountFormula
{
    public const string FormulaName = "FIN+";

    public override string Name => FormulaName;
    public override FormulaStep Step => FormulaStep.Race;
    protected override string CountDescription => "Number of boats that finished race";

    protected override int Count(FormulaContext context) =>
        context.Host.RaceScores(context.Race, context.Series).Count(context.Host.CountsAsFinished);
}
