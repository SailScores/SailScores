using SailScores.Core.Model;

namespace SailScores.Core.Scoring.Formulas;

// COD: meant to score like the code named in ScoreLike. Unfinished: never evaluated, so the
// code keeps its place-based score (docs/Scoring-Quirks.md).
public sealed class ScoreLikeFormula : IScoreCodeFormula
{
    public const string FormulaName = "COD";

    public string Name => FormulaName;
    public FormulaStep Step => FormulaStep.None;

    public decimal? Evaluate(FormulaContext context) => null;

    public string Describe(ScoreCode code) => $"Scored like {code.ScoreLike}";
}
