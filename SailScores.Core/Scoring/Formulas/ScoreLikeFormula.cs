using SailScores.Core.Model;

namespace SailScores.Core.Scoring.Formulas;

// COD: scores like the code named in ScoreLike. Not evaluated itself: the pipeline scores the
// code with that code's formula and formula value (ScoringPipeline.FormulaCode).
public sealed class ScoreLikeFormula : IScoreCodeFormula
{
    public const string FormulaName = "COD";

    public string Name => FormulaName;
    public FormulaStep Step => FormulaStep.None;

    public decimal? Evaluate(FormulaContext context) => null;

    public string Describe(ScoreCode code) => $"Scored like {code.ScoreLike}";
}
