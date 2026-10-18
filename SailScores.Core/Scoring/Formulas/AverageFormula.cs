using System.Linq;
using SailScores.Core.Model;

namespace SailScores.Core.Scoring.Formulas;

// AVE and AVE W: the average of the boat's other scores in the series, rounded to one
// decimal (AVE) or a whole number (AVE W). Used for redress (RDG).
public sealed class AverageFormula : IScoreCodeFormula
{
    public const string Ave = "AVE";
    public const string AveWhole = "AVE W";

    private readonly int _decimals;

    public AverageFormula(string name, int decimals)
    {
        Name = name;
        _decimals = decimals;
    }

    public string Name { get; }
    public FormulaStep Step => FormulaStep.Average;

    public decimal? Evaluate(FormulaContext context)
    {
        var average = Averages.Averaged(context.Host, context.Competitor.CalculatedScores.Values)
            .Average(s => s.ScoreValue) ?? 0m;
        return Averages.Round(average, _decimals);
    }

    public string Describe(ScoreCode code) =>
        _decimals == 0 ? "Average of results rounded to a whole number" : "Average of results";
}
