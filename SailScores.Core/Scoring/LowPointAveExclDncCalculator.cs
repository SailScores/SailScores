using SailScores.Api.Enumerations;
using SailScores.Core.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SailScores.Core.Scoring
{
    public class LowPointAveExclDncCalculator : LowPointAveInclDncCalculator
    {
        public LowPointAveExclDncCalculator(ScoringSystem scoringSystem) : base(scoringSystem)
        {
            CompetitorComparer = new LowPointSeriesCompComparer();
        }

        protected override decimal? GetBasicScore(
            IEnumerable<Score> allScores,
            Score currentScore)
        {
            if(currentScore == null)
            {
                throw new ArgumentNullException(nameof(currentScore));
            }

            decimal? returnScore =
                Convert.ToDecimal(allScores
                    .Count(s =>
                        currentScore.Place.HasValue
                        && s.Race == currentScore.Race
                        && s.Place < currentScore.Place
                        && !ShouldAdjustOtherScores(s)
                        ) + 1);

            returnScore = GetTiedScore(allScores, currentScore) ?? returnScore;

            return returnScore;
        }

        protected override decimal? GetPerfectScore(IEnumerable<Score> allScores, Score currentScore)
        {
            return 1;
        }

        protected override void CalculateTotals(SeriesResults results, IEnumerable<Score> scores)
        {
            results.IsPercentSystem = true;
            results.PercentRequired = ScoringSystem.ParticipationPercent ?? 0m;

            var totalRaceCount = results.Races.Where(r =>
                    ( r.State ?? RaceState.Raced) == RaceState.Raced
                        || r.State == RaceState.Preliminary)
                .Count();
            var requiredRaces = totalRaceCount * ((ScoringSystem.ParticipationPercent ?? 0) / 100m);

            foreach (var comp in results.Competitors)
            {
                var compResults = results.Results[comp];
                var racesParticipated = compResults.CalculatedScores
                    .Where(s => CountsAsStarted(s.Value.RawScore) ||
                           CountsAsParticipation(s.Value.RawScore)).Count();
                compResults.ParticipationPercent = racesParticipated * 100.0m / totalRaceCount;
                var raceCount = compResults
                    .CalculatedScores.Values
                    .Count(s => !s.Discard && (s.ScoreValue ?? 0.0m) != 0.0m);

                compResults.TotalScore = null;
                if (raceCount != 0)
                {
                    compResults.PointsEarned = compResults
                        .CalculatedScores.Values
                        .Where(s => !s.Discard)
                        .Sum(s => s.ScoreValue ?? 0.0m);
                    compResults.Average = compResults.PointsEarned
                            / raceCount;
                    compResults.PointsPossible = raceCount;

                    if (racesParticipated >= requiredRaces)
                    {
                        compResults.TotalScore = compResults.Average;
                    }
                }
            }
        }

        protected override void CalculateOverrides(SeriesResults resultsWorkInProgress, SeriesCompetitorResults compResults)
        {
            base.CalculateOverrides(resultsWorkInProgress, compResults);
            foreach(var race in resultsWorkInProgress.SailedRaces)
            {
                var score = compResults.CalculatedScores[race];
                var scoreCode = GetScoreCode(score.RawScore);
                if (scoreCode != null && ShouldExcludeScore(scoreCode))
                {
                    score.ScoreValue = 0.0m;
                }
                
            }
        }

        private bool ShouldExcludeScore(ScoreCode scoreCode)
        {
            return !(scoreCode.CameToStart ?? true) && (
                scoreCode.Formula.Equals(FINISHERSPLUS_FORMULANAME, CASE_INSENSITIVE)
                || scoreCode.Formula.Equals(CAMETOSTARTPLUS_FORMULANAME, CASE_INSENSITIVE)
                || scoreCode.Formula.Equals(SERIESCOMPETITORS_FORMULANAME, CASE_INSENSITIVE)                
            );
        }

        private bool ShouldExcludeScore(Score rawScore)
        {
            if (GetScoreCode(rawScore) is ScoreCode scoreCode)
            {
                return ShouldExcludeScore(scoreCode);
            }

            return false;
            
        }

        protected override void DiscardScores(
            SeriesResults resultsWorkInProgress,
            SeriesCompetitorResults compResults)
        {
            int numOfDiscards = GetNumberOfDiscards(resultsWorkInProgress, compResults);

            var compResultsOrdered = compResults.CalculatedScores.Values.OrderBy(s => s.ScoreValue / s.PerfectScoreValue)
                .ThenBy(s => s.RawScore.Race.Date)
                .ThenBy(s => s.RawScore.Race.Order)
                .Where(s => CameToStart(s.RawScore) && (GetScoreCode(s.RawScore)?.Discardable ?? true));
            foreach (var score in compResultsOrdered.Take(numOfDiscards))
            {
                score.Discard = true;
            }
        }

        // Not in Base: That one doesn't need competitor info to determine number of discards
        private int GetNumberOfDiscards(SeriesResults resultsWorkInProgress, SeriesCompetitorResults compResults)
        {
            var numOfRaces = compResults.CalculatedScores
                    .Where(s => !ShouldExcludeScore(s.Value.RawScore) ||
                        CountsAsParticipation(s.Value.RawScore)).Count();
            return GetNumberOfDiscards(numOfRaces);
        }



        // There are two scenarios to handle for ties:
        // 1. Place: 2 code: null & Place: 2 code: "TIE"
        // 2. Place: 2 code: null & Place: 3 code: "TIE"
        // That is, the places might or might not match.
        private decimal? GetTiedScore(IEnumerable<Score> allScores, Score currentScore)
        {

            // other results with same place are ties
            int numTied = allScores.Count(s =>
                currentScore.Place.HasValue
                && s.Race == currentScore.Race
                && s.Place == currentScore.Place
                && !ShouldAdjustOtherScores(s));


            int tieBase = currentScore.Place ?? 1;
            if (numTied == 1)
            {
                var tmpScore = currentScore;
                bool tie = false;
                // if current is tie look at previous scores until not a tie.
                if ((GetScoreCode(currentScore?.Code)?.Formula ?? String.Empty) == "TIE")
                {
                    do
                    {
                        numTied++;
                        var previousScore = GetPreviousScore(allScores, tmpScore);
                        tieBase = previousScore?.Place ?? tieBase;
                        tie = (GetScoreCode(previousScore?.Code)?.Formula ?? String.Empty) == "TIE";
                        tmpScore = previousScore;

                    } while (tie);
                }

                //also need to look at next scores to see if they are ties.
                tmpScore = currentScore;
                do
                {
                    var nextScore = GetNextScore(allScores, tmpScore);
                    tie = (GetScoreCode(nextScore?.Code)?.Formula ?? String.Empty) == "TIE";
                    if (tie)
                    {
                        numTied++;
                    }
                    tmpScore = nextScore;

                } while (tie);
            }

            if (numTied > 1)
            {
                int total = 0;
                for (int i = 0; i < numTied; i++)
                {
                    total += (tieBase + i);
                }
                return (decimal)total / (decimal)numTied;
            }

            return null;
        }

        private static Score GetNextScore(IEnumerable<Score> allScores, Score currentScore)
        {
            return allScores.FirstOrDefault(s =>
            s.Race == currentScore.Race
            && s.Place == currentScore.Place + 1);
        }
        private static Score GetPreviousScore(IEnumerable<Score> allScores, Score currentScore)
        {
            return allScores.
                Where(s => s.Race == currentScore.Race
                && s.Place < currentScore.Place)
                .OrderByDescending(s => s.Place)
                .FirstOrDefault();
        }
    }
}
