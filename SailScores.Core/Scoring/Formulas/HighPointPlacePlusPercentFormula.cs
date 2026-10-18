using System;
using System.Linq;
using SailScores.Core.Model;

namespace SailScores.Core.Scoring.Formulas;

// PLC% for high point systems: the points less Formula Value percent (20 if blank) of the
// boats that came to the start, rounded to a whole number, no less than the DNF score.
public sealed class HighPointPlacePlusPercentFormula : IScoreCodeFormula
{
    public string Name => PlacePlusPercentFormula.FormulaName;
    public FormulaStep Step => FormulaStep.Race;

    public decimal? Evaluate(FormulaContext context)
    {
        var dnfScore = context.Host.DnfScore(context.Race, context.Series) ?? 0;
        var fleetSize = context.Host.RaceScores(context.Race, context.Series).Count(context.Host.CameToStart);
        var percentAdjustment = context.Code?.FormulaValue ?? 20m;
        var penalty = Math.Round(fleetSize * percentAdjustment / 100m, MidpointRounding.AwayFromZero);

        return Math.Max(dnfScore, (context.Score.ScoreValue ?? 0) - penalty);
    }

    public string Describe(ScoreCode code) => PlacePlusPercentFormula.Description(code);
}
