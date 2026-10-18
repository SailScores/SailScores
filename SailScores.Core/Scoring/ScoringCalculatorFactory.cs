using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SailScores.Api.Enumerations;
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
            var calculatorType = await GetCalculatorTypeAsync(scoringSystem).ConfigureAwait(false);
            // Keep the resolved type: flattening results reads it to pick the scoring columns.
            scoringSystem.CalculatorType ??= calculatorType;

            return CreateScoringCalculator(calculatorType, scoringSystem);
        }

        public async Task<IScoringCalculator> CreateScoringCalculatorAsync(
            Model.ScoringSystem scoringSystem,
            Model.HandicapSystem handicapSystem,
            IReadOnlyDictionary<(Guid competitorId, DateTime raceDate), decimal> handicapLookup)
        {
            var innerCalculator = await CreateScoringCalculatorAsync(scoringSystem).ConfigureAwait(false);
            return new HandicapScoringCalculator(innerCalculator, handicapSystem, handicapLookup);
        }

        public static IScoringCalculator CreateScoringCalculator(
            ScoringCalculatorType? calculatorType,
            Model.ScoringSystem scoringSystem)
        {
            return ScoringCalculatorRegistry.CreateCalculator(calculatorType, scoringSystem);
        }

        // ScoringService resolves CalculatorType from the root system. Models built
        // elsewhere may not have it, so fall back to walking the parent chain.
        private async Task<ScoringCalculatorType?> GetCalculatorTypeAsync(ScoringSystem scoringSystem)
        {
            if (scoringSystem.CalculatorType.HasValue || scoringSystem.ParentSystemId == null)
            {
                return scoringSystem.CalculatorType;
            }

            var cacheKey = $"ScoringCalculatorType_{scoringSystem.Id}";
            if (_cache.TryGetValue(cacheKey, out ScoringCalculatorType? calculatorType))
            {
                return calculatorType;
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
            calculatorType = currentSystem.CalculatorType;
            _cache.Set(cacheKey, calculatorType, TimeSpan.FromSeconds(15));
            return calculatorType;
        }
    }
}
