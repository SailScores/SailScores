using SailScores.Api.Enumerations;
using SailScores.Core.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SailScores.Core.Scoring
{

    // Scoring system based on
    // https://www.hryra.org/wp-content/uploads/2015/06/Cox-Sprague-Scoring-System.pdf

    public class CoxSpragueCalculator : BaseScoringCalculator
    {
        public CoxSpragueCalculator(ScoringSystem scoringSystem) : base(scoringSystem)
        {
            CompetitorComparer = new CoxSpragueCompComparer();
        }

        protected override decimal? GetBasicScore(IEnumerable<Score> allScores, Score currentScore)
        {
            int starters = allScores.Count(s =>
                s.Race == currentScore.Race
                 && CountsAsStarted(s));


            int baseScore =
                allScores
                    .Count(s =>
                        currentScore.Place.HasValue
                        && s.Race == currentScore.Race
                        && s.Place <= currentScore.Place
                        && !ShouldAdjustOtherScores(s)
                        );

            // removed code to handle race result ties: this should treat both as the same place.

            return CoxSpragueTable.GetScore( baseScore, starters);
        }

        protected override decimal? GetPerfectScore(IEnumerable<Score> allScores, Score currentScore)
        {
            int starters = allScores.Count(s =>
                s.Race == currentScore.Race
                 && CountsAsStarted(s));
            return CoxSpragueTable.GetScore(1, starters);
        }

        // for Cox-Sprague this is most coded results: they depend on number of starters
        protected override void CalculateRaceDependentScores(SeriesResults resultsWorkInProgress, SeriesCompetitorResults compResults)
        {
            
            foreach (var race in resultsWorkInProgress.SailedRaces)
            {
                var score = compResults.CalculatedScores[race];
                var scoreCode = GetScoreCode(score.RawScore);
                if (scoreCode != null && CameToStart(score.RawScore)
                    && scoreCode.Formula != TIE_FORMULANAME)
                {
                    var starters = race.Scores.Count(s => CountsAsStarted(s));
                    score.ScoreValue = CoxSpragueTable.GetScore(starters + 1, starters);
                }
            }
        }

        /// The series score for each boat:
        /// divide the sum of her race scores by the sum of the points
        /// she would have scored if she had placed first in every race in
        /// which she competed
        protected override void CalculateTotals(
            SeriesResults results,
            IEnumerable<Score> scores)
        {
            results.IsPercentSystem = true;
            results.LowerScoreWins = false;
            results.PercentRequired = ScoringSystem.ParticipationPercent;
            var raceCount = results.Races.Where(r => (r.State ?? RaceState.Raced) == RaceState.Raced
                || r.State == RaceState.Preliminary).Count();
            var requiredRaces = raceCount * ((ScoringSystem.ParticipationPercent ?? 0) / 100m);

            Dictionary<Guid, int> starterCounts = new Dictionary<Guid, int>();
            foreach (Race r in results.SailedRaces)
            {
                starterCounts[r.Id] = r.Scores.Count(s => CountsAsStarted(s));
            }
            foreach (var comp in results.Competitors)
            {
                var currentCompResults = results.Results[comp];
                var racesParticipated = currentCompResults.CalculatedScores
                    .Where(s => CountsAsStarted(s.Value.RawScore) ||
                           CountsAsParticipation(s.Value.RawScore)).Count();
                currentCompResults.ParticipationPercent = racesParticipated * 100.0m / raceCount;
                

                // racesToExclude should include discards and DNCs
                var racesToExclude = currentCompResults
                    .CalculatedScores
                    .Where(s => s.Value.Discard ||
                    ( !String.IsNullOrEmpty(s.Value.RawScore.Code) && !(GetScoreCode(s.Value.RawScore.Code).CameToStart ?? false)))
                    .Select(s => s.Key.Id);

                int perfectScore = 0;
                foreach (Race r in results.SailedRaces)
                {
                    if (racesToExclude.Contains(r.Id))
                    {
                        continue;
                    }

                    perfectScore += CoxSpragueTable.GetScore(1, starterCounts[r.Id]);
                }

                var compTotal = currentCompResults
                    .CalculatedScores.Values
                    .Sum(s => !s.Discard ? (s.ScoreValue ?? 0.0m) : 0.0m);
                currentCompResults.TotalScore = null; //we'll replace this below if they
                // completed enough races. Making the null value explicit here, though.

                currentCompResults.PointsEarned = compTotal;
                currentCompResults.PointsPossible = perfectScore;
                if (perfectScore == 0)
                {
                    currentCompResults.Average = 0;
                }
                else
                {
                    currentCompResults.Average = compTotal * 100 / perfectScore;
                }

                if (racesParticipated >= requiredRaces)
                {
                    currentCompResults.TotalScore = currentCompResults.Average;
                }
            }
        }

        // Discards for Cox-Sprague are not straight forward.
        protected override void DiscardScores(
            SeriesResults resultsWorkInProgress,
            SeriesCompetitorResults compResults)
        {
            // Don't use the BaseScoringCalculator's GetNumberOfDiscards method:
            // it gives the number of discards based total races. Cox-Sprague discards
            // based on the number of races the current competitor has sailed.
            int numOfDiscards = GetNumberOfDiscards(resultsWorkInProgress, compResults);

            for (int i = 0; i < numOfDiscards; i++)
            {
                var race = GetNextRaceToDiscard(resultsWorkInProgress, compResults);
                if (race != null)
                {
                    compResults.CalculatedScores[race].Discard = true;
                }
                else
                {
                    break;
                }
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

        // compare, for all races:
        // (TotalScoreForThisComp / (TotalScoreForThisComp - ScoreForThisRace)) -
        // ((TotalPerfectScore - TotalScoreForThisComp) /
        //      (TotalPerfectScore - TotalScoreForThisComp - PerfectScoreForThisRace + ScoreForThisRace))
        private Race GetNextRaceToDiscard(SeriesResults resultsWorkInProgress, SeriesCompetitorResults compResults)
        {
            var compResultsOrdered = compResults.CalculatedScores.Values.OrderBy(s => s.ScoreValue)
                .ThenBy(s => s.RawScore.Race.Date)
                .ThenBy(s => s.RawScore.Race.Order)
                .Where(s => GetScoreCode(s.RawScore)?.Discardable ?? true);

            // calculate Perfect Score for all non discards
            decimal perfectTotal = compResults.CalculatedScores.Values
                .Where(s => !s.Discard && CameToStart(s.RawScore)).Sum(s => s.PerfectScoreValue ?? 0);
            decimal compScore = compResults.CalculatedScores.Values
                .Where(s => !s.Discard && CameToStart(s.RawScore)).Sum(s => s.ScoreValue ?? 0);

            // filter to candidate races to be discarded
            var raceIdsThatMightBeDiscarded = compResults.CalculatedScores.Values.Where(s => !s.Discard
            && CameToStart(s.RawScore)).Select(s => s.RawScore.RaceId);
            var racesThatMightBeDiscards = resultsWorkInProgress.Races.Where(
                r => raceIdsThatMightBeDiscarded.Any(i => i == r.Id));

            Race raceToDiscard = null;
            decimal discardQuotient = 0;

            foreach(var race in racesThatMightBeDiscards)
            {
                decimal thisRacePerfectScore = compResults.CalculatedScores[race].PerfectScoreValue ?? 0;
                decimal thisRaceScore = compResults.CalculatedScores[race].ScoreValue ?? 0;

                decimal thisRaceQuotient = thisRaceScore - (thisRacePerfectScore * compScore / perfectTotal);
                if(thisRaceQuotient < discardQuotient)
                {
                    raceToDiscard = race;
                    discardQuotient = thisRaceQuotient;
                }
            }

            return raceToDiscard;
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

        protected override ScoreCodeSummary GetScoreCodeSummary(string code)
        {
            var codeDef = GetScoreCode(code);

            string possibleFormula;

            if (CountsAsParticipation(codeDef) && !CameToStart(codeDef))
            {
                possibleFormula = "Participation but not scored.";
            }
            else if (!CameToStart(codeDef))
            {
                possibleFormula = "No participation, no score."; 
            } else
            {
                possibleFormula = base.GetScoreCodeSummary(code).Formula;
            }
            if (possibleFormula.Contains("Fixed") && possibleFormula.Contains("0"))
            {
                possibleFormula = "One greater than # came to start.";
            }

            return new ScoreCodeSummary
            {
                Name = codeDef.Name,
                Description = codeDef.Description,
                Formula = possibleFormula
            };

        }
    }
}
