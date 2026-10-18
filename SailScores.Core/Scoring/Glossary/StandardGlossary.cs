using SailScores.Core.Model;

namespace SailScores.Core.Scoring.Glossary;

// Each code's formula as its formula class describes it.
public sealed class StandardGlossary : ICodeGlossary
{
    public string Formula(ScoringWorkspace workspace, ScoreCode code) =>
        GlossaryText.Formula(workspace.Formulas, code);
}
