using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SailScores.Core.Model;
using SailScores.Core.Services;
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

            var baseSystemName = await GetBaseScoringSystemNameAsync(scoringSystem);

            if (baseSystemName.Contains("High Point"))
            {
                return new HighPointPercentageCalculator(scoringSystem);
            }
            else
            {
                return new AppendixACalculator(scoringSystem);
            }
        }

        private async Task<string> GetBaseScoringSystemNameAsync(ScoringSystem scoringSystem)
        {
            if(scoringSystem.ParentSystemId == null)
            {
                return scoringSystem.Name;
            }
            Database.Entities.ScoringSystem currentSystem =
                await _dbContext.ScoringSystems.SingleAsync(s => s.Id == scoringSystem.ParentSystemId); 
            while (currentSystem.ParentSystemId != null)
            {
                currentSystem = await _dbContext.ScoringSystems.SingleAsync(s => s.Id == currentSystem.ParentSystemId);
            }
            return currentSystem.Name;
        }
    }
}
