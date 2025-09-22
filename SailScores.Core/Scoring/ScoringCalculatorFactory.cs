using System;
using System.Threading;
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

            if (baseSystemName.Contains("High Point Percentage"))
            {
                return new HighPointPercentageCalculator(scoringSystem);
            } else if (baseSystemName.Contains("Cox-Sprague"))
            {
                return new CoxSpragueCalculator(scoringSystem);
            } else if (baseSystemName.Contains("Low Point Average"))
            {
                return new LowPointAverageCalculator(scoringSystem);
            }
            else if (baseSystemName.Contains("First = 0"))
            {
                return new AppAAltFirstIsZero(scoringSystem);
            }
            else if (baseSystemName.Contains("First = ")
                && baseSystemName.Contains(".7"))
            {
                return new AppAAltFirstIsPoint7(scoringSystem);
            }
            else if (baseSystemName.Contains("Top X High Point"))
            { 
                return new TopXHighPointCalculator(scoringSystem);
            }
            else if (baseSystemName.Contains("Appendix A") & baseSystemName.Contains("pre-2025"))
            {
                return new AppendixAPre2025Calculator(scoringSystem);
            } else if (baseSystemName.StartsWith("PWA Standard"))
            {
                return new PwaStandardCalculator(scoringSystem);
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
                _cache.Set($"ScoringSystemName_{scoringSystem.Id}", baseSystemName, TimeSpan.FromSeconds(20));
            }
            return baseSystemName;
        }
    }
}
