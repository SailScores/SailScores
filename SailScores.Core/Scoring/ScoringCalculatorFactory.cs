using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using SailScores.Core.Model;
using SailScores.Core.Services;

namespace SailScores.Core.Scoring
{
    public class ScoringCalculatorFactory : IScoringCalculatorFactory
    {
        public IScoringCalculator CreateScoringCalculator(
            Model.ScoringSystem scoringSystem)
        {
            // I expect there will be one base Scoring Calculator per scroing system type.
            // They will use the base system and any overridden rules.

            // So we will need to inspect scoring system to determine base type.
            // but for now, only one type set up, so handle trivially:
            return new AppendixACalculator(scoringSystem);
        }
    }
}
