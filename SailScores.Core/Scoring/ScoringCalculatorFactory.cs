using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SailScores.Core.Model;
using SailScores.Database;

namespace SailScores.Core.Scoring
{
    public class ScoringCalculatorFactory : IScoringCalculatorFactory
    {
        private readonly ISailScoresContext _dbContext;
        private readonly IMemoryCache _cache;

        public ScoringCalculatorFactory(
            ISailScoresContext dbContext,
            IMemoryCache cache)
        {
            _dbContext = dbContext;
            _cache = cache;
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
            }
            else
            {
                return new AppendixACalculator(scoringSystem);
            }
        }

        private async Task<string> GetBaseScoringSystemNameAsync(ScoringSystem scoringSystem)
        {
            string baseSystemName;
            if (!_cache.TryGetValue($"ScoringSystemName_{scoringSystem.Id}", out baseSystemName))
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
                baseSystemName = currentSystem.Name;
                _cache.Set($"ScoringSystemName_{scoringSystem.Id}", baseSystemName, TimeSpan.FromSeconds(30));
            }
            return baseSystemName;
        }
    }
}
