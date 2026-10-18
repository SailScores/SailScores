using System.Linq;

namespace SailScores.Core.Scoring.Formulas;

// CTS+: boats that came to the starting area, plus the Formula Value.
public sealed class CameToStartPlusFormula : CompetitorCountFormula
{
    public const string FormulaName = "CTS+";

    public override string Name => FormulaName;
    public override FormulaStep Step => FormulaStep.Race;
    protected override string CountDescription => "Number of boats that came to starting area";

    protected override int Count(FormulaContext context) =>
        context.Host.RaceScores(context.Race, context.Series).Count(context.Host.CameToStart);
}
