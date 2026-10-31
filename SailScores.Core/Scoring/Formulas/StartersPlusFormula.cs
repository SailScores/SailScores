using System.Linq;

namespace SailScores.Core.Scoring.Formulas;

// STR+: boats that started the race (a place with no code, or a code that counts as started),
// plus the Formula Value. A DNS isn't a starter.
public sealed class StartersPlusFormula : CompetitorCountFormula
{
    public const string FormulaName = "STR+";

    public override string Name => FormulaName;
    public override FormulaStep Step => FormulaStep.Race;
    protected override string CountDescription => "Number of boats that started";

    protected override int Count(FormulaContext context) =>
        context.Host.RaceScores(context.Race, context.Series).Count(context.Host.CountsAsStarted);
}
