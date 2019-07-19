using SailScores.Api.Enumerations;
using SailScores.Core.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SailScores.Core.Scoring
{
    public class AppendixACalculator : BaseScoringCalculator, IScoringCalculator
    {
        public AppendixACalculator(ScoringSystem scoringSystem) : base(scoringSystem)
        {
        }
        

        protected override decimal? GetBasicScore(
            IEnumerable<Score> allScores,
            Score currentScore)
        {
            decimal? returnScore =
                Convert.ToDecimal(allScores
                    .Count(s =>
                        currentScore.Place.HasValue
                        && s.Race == currentScore.Race
                        && s.Place < currentScore.Place
                        && !ShouldAdjustOtherScores(s)
                        ) + 1);

            // if this is one, no tie. (if zero Place doesn't have a value (= coded.))
            int numTied = allScores.Count(s =>
                currentScore.Place.HasValue
                && s.Race == currentScore.Race
                && s.Place == currentScore.Place
                && !ShouldAdjustOtherScores(s));
            if (numTied > 1)
            {
                int total = 0;
                for (int i = 0; i < numTied; i++)
                {
                    total += ((int)currentScore.Place + i);
                }
                returnScore = (decimal)total / (decimal)numTied;
            }

            return returnScore;
        }

    }
}
