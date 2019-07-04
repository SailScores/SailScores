namespace SailScores.Core.Scoring
{
    public interface IScoringCalculatorFactory
    {
        IScoringCalculator CreateScoringCalculator(Model.ScoringSystem scoringSystem);
    }
}