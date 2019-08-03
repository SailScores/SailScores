using System.Threading.Tasks;

namespace SailScores.Core.Scoring
{
    public interface IScoringCalculatorFactory
    {
        Task<IScoringCalculator> CreateScoringCalculatorAsync(Model.ScoringSystem scoringSystem);
    }
}