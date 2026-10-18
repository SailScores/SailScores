namespace SailScores.Core.Scoring.Formulas;

// SER+: boats in the series, plus the Formula Value.
public sealed class SeriesCompetitorsPlusFormula : CompetitorCountFormula
{
    public const string FormulaName = "SER+";

    public override string Name => FormulaName;
    public override FormulaStep Step => FormulaStep.Series;
    protected override string CountDescription => "Number of boats entered in series";

    protected override int Count(FormulaContext context) => context.Series.Competitors.Count;
}
