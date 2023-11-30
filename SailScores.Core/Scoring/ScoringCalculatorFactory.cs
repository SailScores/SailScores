using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SailScores.Core.Model;
using SailScores.Database;

namespace SailScores.Core.Scoring
{
    public class ScoringCalculatorFactory : IScoringCalculatorFactory
    {
        private readonly ISailScoresContext _dbContext;

        public ScoringCalculatorFactory(
            ISailScoresContext dbContext)
        {
            _dbContext = dbContext;
        }
        public async Task<IScoringCalculator> CreateScoringCalculatorAsync(
            Model.ScoringSystem scoringSystem)
        {

            var baseSystemName = await GetBaseScoringSystemNameAsync(scoringSystem).ConfigureAwait(false);

            if (baseSystemName.Contains("High Point"))
            {
                return new HighPointPercentageCalculator(scoringSystem);
            } else if (baseSystemName.Contains("Cox-Sprague"))
            {
                return new CoxSpragueCalculator(scoringSystem);
            } else if (baseSystemName.Contains("Low Point Average"))
            {
                return new LowPointAverageCalculator(scoringSystem);
            }
            else
            {
                return new AppendixACalculator(scoringSystem);
            }
        }

        private async Task<string> GetBaseScoringSystemNameAsync(ScoringSystem scoringSystem)
        {
            if (scoringSystem.ParentSystemId == null)
            {
                return scoringSystem.Name;
            }
            Database.Entities.ScoringSystem currentSystem =
                await _dbContext.ScoringSystems.SingleAsync(s => s.Id == scoringSystem.ParentSystemId)
                .ConfigureAwait(false);
            while (currentSystem.ParentSystemId != null)
            {
                currentSystem = await _dbContext.ScoringSystems
                    .SingleAsync(s => s.Id == currentSystem.ParentSystemId)
                    .ConfigureAwait(false);
            }
            return currentSystem.Name;
        }
    }
}
