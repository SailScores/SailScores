using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SailScores.Core.Scoring
{
    public interface IScoringCalculatorFactory
    {
        Task<IScoringCalculator> CreateScoringCalculatorAsync(Model.ScoringSystem scoringSystem);

        Task<IScoringCalculator> CreateScoringCalculatorAsync(
            Model.ScoringSystem scoringSystem,
            Model.HandicapSystem handicapSystem,
            IReadOnlyDictionary<(Guid competitorId, DateTime raceDate), decimal> handicapLookup);
    }
}
