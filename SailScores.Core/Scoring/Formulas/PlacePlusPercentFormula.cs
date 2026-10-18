using System;
using SailScores.Core.Model;

namespace SailScores.Core.Scoring.Formulas;

// PLC% for low point systems: the place plus Formula Value percent (20 if blank) of the
// DNF score, no more than the DNF score. RRS 44.3(c) penalties such as ZFP and SCP.
public sealed class PlacePlusPercentFormula : IScoreCodeFormula
{
    public const string FormulaName = "PLC%";

    // Decimal places the penalty is rounded to, away from zero.
    public const int Rounding2025 = 1;
    public const int RoundingPre2025 = 0;

    private readonly int _decimals;

    public PlacePlusPercentFormula(int decimals)
    {
        _decimals = decimals;
    }

    public string Name => FormulaName;
    public FormulaStep Step => FormulaStep.Race;

    public decimal? Evaluate(FormulaContext context)
    {
        var dnfScore = context.Host.DnfScore(context.Race, context.Series) ?? 1;
        var percentAdjustment = context.Code?.FormulaValue ?? 20m;
        var penalty = Math.Round(dnfScore * percentAdjustment / 100m, _decimals, MidpointRounding.AwayFromZero);

        return Math.Min(dnfScore, penalty + (context.Score.ScoreValue ?? context.Score.RawScore.Place ?? 0));
    }

    public string Describe(ScoreCode code) => Description(code);

    internal static string Description(ScoreCode code) =>
        $"Place + penalty ({code.FormulaValue?.ToString("0.###")}% of DNF score)";
}
