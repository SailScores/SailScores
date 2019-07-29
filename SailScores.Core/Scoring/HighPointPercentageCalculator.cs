using SailScores.Api.Enumerations;
using SailScores.Core.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SailScores.Core.Scoring
{

    // Scoring system based on
    // https://www.ussailing.org/competition/rules-officiating/racing-rules/scoring-a-long-series/

    public class HighPointPercentageCalculator : BaseScoringCalculator, IScoringCalculator
    {
        public HighPointPercentageCalculator(ScoringSystem scoringSystem) : base(scoringSystem)
        {
        }

        protected override decimal? GetBasicScore(IEnumerable<Score> allScores, Score currentScore)
        {
            int competitorsInRace = allScores.Count(s =>
                s.Race == currentScore.Race
                 && CameToStart(s));


            decimal? baseScore =
                Convert.ToDecimal(allScores
                    .Count(s =>
                        currentScore.Place.HasValue
                        && s.Race == currentScore.Race
                        && s.Place < currentScore.Place
                        && !ShouldAdjustOtherScores(s)
                        ) - 1);

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
                baseScore = (decimal)total / (decimal)numTied;
            }

            return competitorsInRace - baseScore;
        }

        /// The series score for each boat will be a percentage calculated as
        /// follows: divide the sum of her race scores by the sum of the points
        /// she would have scored if she had placed first in every race in
        /// which she competed; multiply the result by 100.2 The qualified boat
        /// with the highest series score is the winner, and others are ranked
        /// accordingly.
        protected override void CalculateTotals(
            SeriesResults results,
            IEnumerable<Score> allScores)
        {
            foreach ( var comp in results.Competitors)
            {
                var currentCompResults = results.Results[comp];
                var racesToExclude = currentCompResults
                    .CalculatedScores
                    .Where(s => s.Value.Discard)
                    .Select(s => s.Key.Id);
                var perfectScore = allScores.Where(s => !racesToExclude.Contains(s.RaceId))
                    .Count(s => CameToStart(s));
                var compTotal = currentCompResults
                    .CalculatedScores.Values
                    .Sum(s => !s.Discard ? (s.ScoreValue ?? 0.0m) : 0.0m);

                currentCompResults.TotalScore = compTotal * 100 / perfectScore;

            }
        }

        protected override void CalculateOverrides(SeriesResults resultsWorkInProgress, SeriesCompetitorResults compResults)
        {
            foreach (var race in resultsWorkInProgress.SailedRaces)
            {
                var score = compResults.CalculatedScores[race];
                var defaultScore = GetDefaultScore(race, resultsWorkInProgress);
                if (score?.ScoreValue != null && score.ScoreValue < defaultScore)
                {
                    score.ScoreValue = defaultScore;
                }
            }
        }

        protected override decimal? GetPenaltyScore(CalculatedScore score, Race race, ScoreCode scoreCode)
        {
            var dnfScore = GetDnfScore(race) ?? 0;
            var fleetSize = race.Scores.Where(s => CameToStart(s)).Count();
            var percentAdjustment = Convert.ToDecimal(scoreCode?.FormulaValue ?? 20);
            var percent = Math.Round(fleetSize * percentAdjustment / 100m, MidpointRounding.AwayFromZero);

            return Math.Max(dnfScore, (score.ScoreValue ?? 0) - percent);
        }
    }
}
