using System.Linq;
using SailScores.Core.Model;

namespace SailScores.Core.Scoring.Formulas;

// AVE ND and AVE W ND: the average of the boat's scores that wouldn't be discarded, rounded
// to one decimal or a whole number.
//
// Quirk: it averages the boat's best (races − average-scored races − expected discards)
// scores, not the scores left after the discards actually chosen. When no scores are left,
// it takes the single best score (docs/Scoring-Quirks.md).
public sealed class AverageNonDiscardedFormula : IScoreCodeFormula
{
    public const string AveNonDiscarded = "AVE ND";
    public const string AveWholeNonDiscarded = "AVE W ND";

    private readonly int _decimals;

    public AverageNonDiscardedFormula(string name, int decimals)
    {
        Name = name;
        _decimals = decimals;
    }

    public string Name { get; }
    public FormulaStep Step => FormulaStep.Average;

    public decimal? Evaluate(FormulaContext context)
    {
        var host = context.Host;
        var scores = context.Competitor.CalculatedScores.Values;
        int averageScored = scores.Count(s => Averages.IsAverage(host, s));
        int nonDiscardAverages = scores.Count(s => host.FormulaFor(s.RawScore.Code) is AverageNonDiscardedFormula);
        int discards = host.ExpectedDiscards(context.Competitor);

        var best = Averages.Averaged(host, scores).OrderBy(s => s.ScoreValue);
        decimal average = scores.Count - discards <= nonDiscardAverages
            ? best.FirstOrDefault()?.ScoreValue ?? 0m
            : best.Take(scores.Count - averageScored - discards).Average(s => s.ScoreValue) ?? 0m;
        return Averages.Round(average, _decimals);
    }

    public string Describe(ScoreCode code) =>
        _decimals == 0
            ? "Average of non-discarded results rounded to a whole number"
            : "Average of non-discarded results";
}
