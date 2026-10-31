using SailScores.Core.Model;
using SailScores.Core.Scoring.Totals;

namespace SailScores.Core.Scoring.Glossary;

// Low point averages that leave some codes out of the average: those show as "Excluded".
public sealed class NotAveragedExcludedGlossary : ICodeGlossary
{
    private readonly AveragedScores _averaged;

    public NotAveragedExcludedGlossary(AveragedScores averaged)
    {
        _averaged = averaged;
    }

    public string Formula(ScoringWorkspace workspace, ScoreCode code) =>
        (code?.IncludeInAverage ?? _averaged.IncludesByDefault(code, workspace.Host.FormulaFor(code?.Name)))
            ? GlossaryText.Formula(workspace.Formulas, code)
            : GlossaryText.Excluded(code);
}
