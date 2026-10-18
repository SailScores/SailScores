using System.Linq;
using SailScores.Core.Model;

namespace SailScores.Core.Scoring.Formulas;

// AVE P and AVE W P: the average of the boat's scores in races before this one, rounded to
// one decimal or a whole number.
public sealed class AveragePriorFormula : IScoreCodeFormula
{
    public const string AvePrior = "AVE P";
    public const string AveWholePrior = "AVE W P";

    private readonly int _decimals;

    public AveragePriorFormula(string name, int decimals)
    {
        Name = name;
        _decimals = decimals;
    }

    public string Name { get; }
    public FormulaStep Step => FormulaStep.Average;

    public decimal? Evaluate(FormulaContext context)
    {
        var race = context.Race;
        var prior = context.Competitor.CalculatedScores
            .Where(s => s.Key.Date < race.Date || (s.Key.Date == race.Date && s.Key.Order < race.Order))
            .Select(s => s.Value);

        var average = Averages.Averaged(context.Host, prior).Average(s => s.ScoreValue) ?? 0m;
        return Averages.Round(average, _decimals);
    }

    public string Describe(ScoreCode code) =>
        _decimals == 0
            ? "Average of results in prior races rounded to a whole number"
            : "Average of results in prior races";
}
