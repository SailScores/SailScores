using System;
using System.Collections.Generic;
using System.Linq;
using SailScores.Core.Model;

namespace SailScores.Core.Scoring.Formulas;

// The formulas a calculator scores codes with, found by name ignoring case.
public sealed class FormulaSet
{
    // What every calculator uses unless it replaces a formula.
    public static readonly FormulaSet Standard = new(
        new FixedFormula(),
        new ManualFormula(),
        new FinishersPlusFormula(),
        new CameToStartPlusFormula(),
        new StartersPlusFormula(),
        new SeriesCompetitorsPlusFormula(),
        new PlacePlusPercentFormula(PlacePlusPercentFormula.Rounding2025),
        new AverageFormula(AverageFormula.Ave, decimals: 1),
        new AverageFormula(AverageFormula.AveWhole, decimals: 0),
        new AverageNonDiscardedFormula(AverageNonDiscardedFormula.AveNonDiscarded, decimals: 1),
        new AverageNonDiscardedFormula(AverageNonDiscardedFormula.AveWholeNonDiscarded, decimals: 0),
        new AveragePriorFormula(AveragePriorFormula.AvePrior, decimals: 1),
        new AveragePriorFormula(AveragePriorFormula.AveWholePrior, decimals: 0),
        new TieFormula(),
        new ScoreLikeFormula());

    // High point systems (high point percentage, Cox-Sprague, Top X): PLC% takes points off.
    public static readonly FormulaSet HighPoint = Standard.With(new HighPointPlacePlusPercentFormula());

    private readonly Dictionary<string, IScoreCodeFormula> _formulas;

    private FormulaSet(params IScoreCodeFormula[] formulas)
    {
        _formulas = formulas.ToDictionary(f => f.Name, StringComparer.OrdinalIgnoreCase);
    }

    public IEnumerable<IScoreCodeFormula> All => _formulas.Values;

    // A copy with these formulas replacing any of the same name.
    public FormulaSet With(params IScoreCodeFormula[] replacements) =>
        new(_formulas.Values
            .Where(f => !replacements.Any(r => r.Name.Equals(f.Name, StringComparison.OrdinalIgnoreCase)))
            .Concat(replacements)
            .ToArray());

    // Null for a blank or unknown formula.
    public IScoreCodeFormula Find(string formulaName) =>
        formulaName != null && _formulas.TryGetValue(formulaName, out var formula) ? formula : null;

    public IScoreCodeFormula Find(ScoreCode code) => Find(code?.Formula);
}
