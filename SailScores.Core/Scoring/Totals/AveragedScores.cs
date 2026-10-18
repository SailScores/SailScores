using SailScores.Core.Model;
using SailScores.Core.Scoring.Formulas;

namespace SailScores.Core.Scoring.Totals;

// Which scores count in a low point average when the score code doesn't say (a code's
// IncludeInAverage, when set, always wins).
public sealed class AveragedScores
{
    // Every score.
    public static readonly AveragedScores All = new(excludeCountsNotAtStart: false);

    // Not codes that didn't come to the start and score a count of boats (FIN+, CTS+, SER+):
    // DNC and the like (low point average without DNCs).
    public static readonly AveragedScores ExceptCountsNotAtStart = new(excludeCountsNotAtStart: true);

    private readonly bool _excludeCountsNotAtStart;

    private AveragedScores(bool excludeCountsNotAtStart)
    {
        _excludeCountsNotAtStart = excludeCountsNotAtStart;
    }

    // code is null for a score without a code.
    public bool IncludesByDefault(ScoreCode code, FormulaSet formulas)
    {
        if (!_excludeCountsNotAtStart || code == null)
        {
            return true;
        }
        return (code.CameToStart ?? true) || formulas.Find(code) is not CompetitorCountFormula;
    }
}
