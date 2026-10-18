using SailScores.Core.Model;

namespace SailScores.Core.Scoring.Glossary;

// Percentage systems that leave codes which didn't come to the start out of the perfect
// score (high point percentage): those show as "Excluded".
public sealed class NotAtStartExcludedGlossary : ICodeGlossary
{
    public string Formula(ScoringWorkspace workspace, ScoreCode code) =>
        GlossaryText.CameToStart(code)
            ? GlossaryText.Formula(workspace.Formulas, code)
            : GlossaryText.Excluded(code);
}
