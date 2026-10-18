using SailScores.Core.Model;

namespace SailScores.Core.Scoring.Glossary;

// The "formula" text shown for each score code used in a series' results, e.g.
// "Number of boats entered in series + 1" or "Excluded".
public interface ICodeGlossary
{
    string Formula(ScoringWorkspace workspace, ScoreCode code);
}
