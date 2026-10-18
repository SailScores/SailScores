using SailScores.Core.Model;

namespace SailScores.Core.Scoring.Glossary;

// Cox-Sprague: codes that didn't come to the start are excluded; a code fixed at 0 shows as
// one behind the last starter.
public sealed class CoxSpragueGlossary : ICodeGlossary
{
    private static readonly NotAtStartExcludedGlossary NotAtStart = new();

    public string Formula(ScoringWorkspace workspace, ScoreCode code) =>
        GlossaryText.FixedAtZeroAsOneBehindLast(NotAtStart.Formula(workspace, code));
}
