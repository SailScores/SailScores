using SailScores.Api.Enumerations;
using SailScores.Core.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SailScores.Core.Scoring
{

    // Scoring system based on
    // https://www.ussailing.org/competition/rules-officiating/racing-rules/scoring-a-long-series/

    public class HighPointPercentageCalculator : BaseScoringCalculator
    {
        public HighPointPercentageCalculator(ScoringSystem scoringSystem) : base(scoringSystem)
        {
            CompetitorComparer = new HighPointSeriesCompComparer();
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
                        ));

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
                baseScore = ((decimal)total / (decimal)numTied) - 1;
            }

            return competitorsInRace - baseScore;
        }

        protected override decimal? GetPerfectScore(IEnumerable<Score> allScores, Score currentScore)
        {
            int competitorsInRace = allScores.Count(s =>
                s.Race == currentScore.Race
                 && CameToStart(s));
            return competitorsInRace;
        }

        /// The series score for each boat will be a percentage calculated as
        /// follows: divide the sum of her race scores by the sum of the points
        /// she would have scored if she had placed first in every race in
        /// which she competed; multiply the result by 100. The qualified boat
        /// with the highest series score is the winner, and others are ranked
        /// accordingly.
        protected override void CalculateTotals(
            SeriesResults results,
            IEnumerable<Score> scores)
        {
            results.IsPercentSystem = true;
            results.PercentRequired = ScoringSystem.ParticipationPercent;
            var raceCount = results.Races.Where(r => (r.State ?? RaceState.Raced) == RaceState.Raced
                || r.State == RaceState.Preliminary).Count();
            var requiredRaces = raceCount * ((ScoringSystem.ParticipationPercent ?? 0) / 100m);
            foreach (var comp in results.Competitors)
            {
                var currentCompResults = results.Results[comp];
                var racesParticipated = currentCompResults.CalculatedScores
                    .Where(s => CountsAsStarted(s.Value.RawScore) ||
                                CountsAsParticipation(s.Value.RawScore)).Count();
                currentCompResults.ParticipationPercent = racesParticipated * 100m / raceCount;
                if (racesParticipated < requiredRaces)
                {
                    currentCompResults.TotalScore = null;
                }
                else
                {
                    // racesToExclude should include discards and DNCs
                    var racesToExclude = currentCompResults
                        .CalculatedScores
                        .Where(s => s.Value.Discard ||
                        ( !String.IsNullOrEmpty(s.Value.RawScore.Code) && !(GetScoreCode(s.Value.RawScore.Code).CameToStart ?? false)))
                        .Select(s => s.Key.Id);
                    var perfectScore = scores.Where(s => !racesToExclude.Contains(s.RaceId))
                        .Count(s => CameToStart(s));
                    var compTotal = currentCompResults
                        .CalculatedScores.Values
                        .Sum(s => !s.Discard ? (s.ScoreValue ?? 0.0m) : 0.0m);

                    currentCompResults.PointsEarned = compTotal;
                    currentCompResults.PointsPossible = perfectScore;
                    if (perfectScore > 0)
                    {
                        currentCompResults.TotalScore = compTotal * 100 / perfectScore;
                    }
                    else
                    {
                        currentCompResults.TotalScore = 0;
                    }
                }
            }
        }

        protected override void DiscardScores(
            SeriesResults resultsWorkInProgress,
            SeriesCompetitorResults compResults)
        {
            int numOfDiscards = GetNumberOfDiscards(resultsWorkInProgress, compResults);

            var compResultsOrdered = compResults.CalculatedScores.Values.OrderBy(s => s.ScoreValue / s.PerfectScoreValue)
                .ThenBy(s => s.RawScore.Race.Date)
                .ThenBy(s => s.RawScore.Race.Order)
                .Where(s => CameToStart(s.RawScore) && ( GetScoreCode(s.RawScore)?.Discardable ?? true));
            foreach (var score in compResultsOrdered.Take(numOfDiscards))
            {
                score.Discard = true;
            }
        }

        // Not in Base: That one doesn't need competitor info to determine number of discards
        private int GetNumberOfDiscards(SeriesResults resultsWorkInProgress, SeriesCompetitorResults compResults)
        {
            var numOfRaces = compResults.CalculatedScores
                    .Where(s => CountsAsStarted(s.Value.RawScore) ||
                        CountsAsParticipation(s.Value.RawScore)).Count();
            return GetNumberOfDiscards(numOfRaces);
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
