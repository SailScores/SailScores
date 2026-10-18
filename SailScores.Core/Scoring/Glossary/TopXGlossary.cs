using SailScores.Core.Model;

namespace SailScores.Core.Scoring.Glossary;

// Top X and VARC: codes that didn't come to the start score nothing (saying whether they
// count as participation); a code fixed at 0 shows as one behind the last starter.
public sealed class TopXGlossary : ICodeGlossary
{
    public string Formula(ScoringWorkspace workspace, ScoreCode code)
    {
        string formula;
        if (GlossaryText.CountsAsParticipation(code) && !GlossaryText.CameToStart(code))
        {
            formula = "Participation but not scored.";
        }
        else if (!GlossaryText.CameToStart(code))
        {
            formula = "No participation, no score.";
        }
        else
        {
            formula = GlossaryText.Formula(workspace.Formulas, code);
        }
        return GlossaryText.FixedAtZeroAsOneBehindLast(formula);
    }
}
