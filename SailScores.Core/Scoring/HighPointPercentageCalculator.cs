using SailScores.Api.Enumerations;
using SailScores.Core.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SailScores.Core.Scoring
{
    public class HighPointPercentageCalculator : BaseScoringCalculator, IScoringCalculator
    {

        public HighPointPercentageCalculator(ScoringSystem scoringSystem) : base(scoringSystem)
        {
        }


        private void SetScores(SeriesResults resultsWorkInProgress, IEnumerable<Score> scores)
        {
            ValidateScores(resultsWorkInProgress, scores);
            ClearRawScores(scores);
            foreach (var comp in resultsWorkInProgress.Competitors)
            {
                SeriesCompetitorResults compResults = GenerateBasicScores(comp, scores);
                CalculateCodedResults(resultsWorkInProgress, compResults);
                DiscardScores(resultsWorkInProgress, compResults);
                CalculateTotal(compResults);

                resultsWorkInProgress.Results[comp] = compResults;
            }
            CalculateRanks(resultsWorkInProgress);
            resultsWorkInProgress.Competitors = ReorderCompetitors(resultsWorkInProgress);
        }


        private IList<Competitor> ReorderCompetitors(SeriesResults results)
        {
            return results.Competitors.OrderBy(c => results.Results[c].Rank).ToList();
        }

        private void CalculateRanks(SeriesResults resultsWorkInProgress)
        {
            var orderedComps = resultsWorkInProgress.Results.Values
                .OrderBy(s => s, new SeriesCompetitorResultComparer());

            int i = 1;
            foreach (var comp in orderedComps)
            {
                comp.Rank = i;

                i++;
            }
        }

        private void CalculateCodedResults(SeriesResults resultsWorkInProgress, SeriesCompetitorResults compResults)
        {
            //Fill in DNCs
            foreach (var race in resultsWorkInProgress.SailedRaces)
            {
                if (!compResults.CalculatedScores.ContainsKey(race))
                {
                    compResults.CalculatedScores.Add(race,
                        new CalculatedScore
                        {
                            RawScore = new Score
                            {
                                Code = DEFAULT_CODE,
                                Competitor = compResults.Competitor,
                                Race = race
                            }
                        });
                }
            }

            CalculateRaceDependentScores(resultsWorkInProgress, compResults);
            CalculateSeriesDependentScores(resultsWorkInProgress, compResults);
        }


        private void CalculateRaceDependentScores(SeriesResults resultsWorkInProgress, SeriesCompetitorResults compResults)
        {
            //calculate non-average codes first
            foreach (var race in resultsWorkInProgress.SailedRaces)
            {
                var score = compResults.CalculatedScores[race];
                var scoreCode = GetScoreCode(score.RawScore);
                if (scoreCode != null)
                {
                    if (IsTrivialCalculation(scoreCode))
                    {
                        score.ScoreValue = GetTrivialScoreValue(score);
                    } else if (IsRaceBasedValue(scoreCode))
                    {
                        score.ScoreValue = CalculateRaceBasedValue(score, race);
                    }
                }
            }
        }

        private void CalculateSeriesDependentScores(SeriesResults resultsWorkInProgress, SeriesCompetitorResults compResults)
        {
            foreach (var race in resultsWorkInProgress.SailedRaces)
            {
                var score = compResults.CalculatedScores[race];
                var scoreCode = GetScoreCode(score.RawScore);
                if (score != null && IsSeriesBasedScore(scoreCode))
                {

                    switch (scoreCode.Formula.ToUpperInvariant())
                    {
                        case AVERAGE_FORMULANAME:
                            score.ScoreValue = CalculateAverage(compResults);
                            break;
                        case AVE_AFTER_DISCARDS_FORMULANAME:
                            score.ScoreValue = CalculateAverageNoDiscards(compResults);
                            break;
                        case AVE_PRIOR_RACES_FORMULANAME:
                            score.ScoreValue = CalculateAverageOfPrior(compResults, race);
                            break;
                        case SERIESCOMPETITORS_FORMULANAME:
                            score.ScoreValue = GetNumberOfCompetitors(resultsWorkInProgress) + (scoreCode.FormulaValue ?? 0);
                            break;
                    }

                }
            }
        }

        private int GetNumberOfCompetitors(SeriesResults seriesResults)
        {
            return seriesResults.Competitors.Count();
        }

        private bool IsSeriesBasedScore(ScoreCode scoreCode)
        {
            // defaults to false if not a coded score.
            string formula = scoreCode?.Formula ?? String.Empty;
            bool average = formula.Equals(AVERAGE_FORMULANAME, CASE_INSENSITIVE)
                || formula.Equals(AVE_AFTER_DISCARDS_FORMULANAME, CASE_INSENSITIVE)
                || formula.Equals(AVE_PRIOR_RACES_FORMULANAME, CASE_INSENSITIVE);
            bool seriesCompPlus = scoreCode?.Formula?.Equals(SERIESCOMPETITORS_FORMULANAME, CASE_INSENSITIVE)
                ?? false;
            return average || seriesCompPlus;
        }

        private bool IsTrivialCalculation(ScoreCode scoreCode)
        {
            return scoreCode.Formula.Equals(MANUAL_FORMULANAME, CASE_INSENSITIVE);
        }

        private decimal? GetTrivialScoreValue(CalculatedScore score)
        {
            if (score.RawScore.Place.HasValue)
            {
                return Convert.ToDecimal(score.RawScore.Place);
            }
            return null;
        }

        private bool IsRaceBasedValue(ScoreCode scoreCode)
        {
            return scoreCode.Formula.Equals(FINISHERSPLUS_FORMULANAME, CASE_INSENSITIVE)
                || scoreCode.Formula.Equals(PLACEPLUSPERCENT_FORMULANAME, CASE_INSENSITIVE)
                || scoreCode.Formula.Equals(CAMETOSTARTPLUS_FORMULANAME, CASE_INSENSITIVE);
        }

        private decimal? CalculateRaceBasedValue(CalculatedScore score, Race race)
        {
            var scoreCode = GetScoreCode(score.RawScore);
            switch (scoreCode.Formula.ToUpperInvariant())
            {
                case FINISHERSPLUS_FORMULANAME:
                    return race.Scores.Where(s => CountsAsStarted(s)).Count() + 
                        scoreCode.FormulaValue;
                case CAMETOSTARTPLUS_FORMULANAME:
                    return race.Scores.Where(s => CameToStart(s)).Count() +
                        scoreCode.FormulaValue;
                case PLACEPLUSPERCENT_FORMULANAME:
                    return GetPenaltyScore(score, race, scoreCode);
            }
            throw new InvalidOperationException("Score code definition issue with race based score code.");
        }

        private decimal? GetPenaltyScore(CalculatedScore score, Race race, ScoreCode scoreCode)
        {
            var dnfScore = GetDnfScore(race) ?? 1;
            var percentAdjustment = Convert.ToDecimal(scoreCode?.FormulaValue ?? 20);
            var percent = Math.Round(dnfScore * percentAdjustment / 100m, MidpointRounding.AwayFromZero);
            return Math.Min(dnfScore, percent + (score.ScoreValue ?? score.RawScore.Place ?? 0));
        }

        private decimal? GetDnfScore(Race race)
        {
            var dnfCode = GetScoreCode(DNF_SCORENAME);
            return race.Scores.Where(s => CountsAsStarted(s)).Count() +
                        dnfCode.FormulaValue;
        }

        private decimal? CalculateAverage(
            SeriesCompetitorResults compResults)
        {
            int numAverages = compResults.CalculatedScores
                    .Values.Count(s =>
                        IsAverage(s.RawScore.Code));

            var average = compResults.CalculatedScores.Values
                .Where(s => (s.ScoreValue ?? 0m) != 0m && !IsAverage(s.RawScore.Code))
                .Average(s => s.ScoreValue) ?? 0m;

            return Math.Round(average, 1, MidpointRounding.AwayFromZero);

        }
        private decimal? CalculateAverageNoDiscards(
            SeriesCompetitorResults compResults)
        {
            int numAverages = compResults.CalculatedScores
                    .Values.Count(s =>
                        IsAverage(s.RawScore.Code));
            int discards = GetNumberOfDiscards(compResults.CalculatedScores.Count);

            var average = compResults.CalculatedScores.Values
                .Where(s => (s.ScoreValue ?? 0m) != 0m && !IsAverage(s.RawScore.Code))
                .OrderBy(s => s.ScoreValue)
                .Take(compResults.CalculatedScores.Count - numAverages - discards)
                .Average(s => s.ScoreValue) ?? 0m;

            return Math.Round(average, 1, MidpointRounding.AwayFromZero);

        }

        private decimal? CalculateAverageOfPrior(
            SeriesCompetitorResults compResults,
            Race race)
        {
            var beforeDate = race.Date;
            var beforeOrder = race.Order;

            var racesToUse = compResults.CalculatedScores.Keys
                .Where(r => r.Date < beforeDate || (r.Date == beforeDate && r.Order < beforeOrder));
            var resultsToUse = compResults.CalculatedScores
                .Where(s => racesToUse.Contains(s.Key))
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            int numAverages = resultsToUse
                    .Values.Count(s =>
                        IsAverage(s.RawScore.Code));
            var average = resultsToUse.Values
                .Where(s => (s.ScoreValue ?? 0m) != 0m && !IsAverage(s.RawScore.Code))
                .Average(s => s.ScoreValue) ?? 0m;

            return Math.Round(average, 1, MidpointRounding.AwayFromZero);

        }

        private bool IsAverage(string code)
        {
            if (String.IsNullOrWhiteSpace(code))
            {
                return false;
            }
            var scoreCode = GetScoreCode(code);
            return scoreCode.Formula.Equals(AVERAGE_FORMULANAME, CASE_INSENSITIVE)
                || scoreCode.Formula.Equals(AVE_AFTER_DISCARDS_FORMULANAME, CASE_INSENSITIVE)
                || scoreCode.Formula.Equals(AVE_PRIOR_RACES_FORMULANAME, CASE_INSENSITIVE);
        }

        private bool CountsAsStarted(Score s)
        {
            if (String.IsNullOrWhiteSpace(s.Code) &&
                (s.Place ?? 0) != 0)
            {
                return true;
            }
            var scoreCode = GetScoreCode(s);
            return scoreCode.Started ?? false;
        }

        private bool CameToStart(Score s)
        {
            if (String.IsNullOrWhiteSpace(s.Code) &&
                (s.Place ?? 0) != 0)
            {
                return true;
            }
            var scoreCode = GetScoreCode(s);
            return scoreCode.CameToStart ?? false;
        }

        private void CalculateTotal(SeriesCompetitorResults compResults)
        {
            compResults.TotalScore = compResults.CalculatedScores.Values.Sum(s => !s.Discard ? ( s.ScoreValue ?? 0.0m) : 0.0m);
        }

        private void DiscardScores(SeriesResults resultsWorkInProgress, SeriesCompetitorResults compResults)
        {
            int numOfDiscards = GetNumberOfDiscards(resultsWorkInProgress);

            var compResultsOrdered = compResults.CalculatedScores.Values.OrderByDescending(s => s.ScoreValue)
                .ThenBy(s => s.RawScore.Race.Date)
                .ThenBy(s => s.RawScore.Race.Order)
                .Where(s => GetScoreCode(s.RawScore)?.Discardable ?? true);
            foreach (var score in compResultsOrdered.Take(numOfDiscards))
            {
                score.Discard = true;
            }
        }

        private int GetNumberOfDiscards(SeriesResults resultsWorkInProgress)
        {
            return GetNumberOfDiscards(resultsWorkInProgress.GetSailedRaceCount());

        }
        private int GetNumberOfDiscards(int numberOfRaces)
        {
            if(numberOfRaces == 0)
            {
                return 0;
            }
            var discardStrings = _scoringSystem.DiscardPattern.Split(',');
            string selectedString;
            if(numberOfRaces > discardStrings.Length)
            {
                 selectedString = discardStrings[discardStrings.Length - 1];
            } else
            {
                selectedString = discardStrings[numberOfRaces - 1];
            }

            return int.Parse(selectedString);
        }

        
        private SeriesCompetitorResults GenerateBasicScores(Competitor comp, IEnumerable<Score> scores)
        {
            var returnResults = new SeriesCompetitorResults
            {
                Competitor = comp,
                CalculatedScores = new Dictionary<Race, CalculatedScore>()
            };
            foreach (var score in scores.Where(s => s.Competitor == comp))
            {
                if((score.Race?.State ?? RaceState.Raced) != RaceState.Raced) {
                    continue;
                }
                returnResults.CalculatedScores[score.Race] = new CalculatedScore
                {
                    Discard = false,
                    RawScore = score,
                    ScoreValue = score.Place
                };
                returnResults.CalculatedScores[score.Race].ScoreValue =
                    scores
                        .Count(s =>
                            score.Place.HasValue
                            && s.Race == score.Race
                            && s.Place < score.Place
                            && !ShouldAdjustOtherScores(s)
                            ) + 1;

                // if this is one, no tie. (if zero Place doesn't have a value (= coded.))
                int numTied = scores.Count(s =>
                    score.Place.HasValue
                    && s.Race == score.Race
                    && s.Place == score.Place
                    && !ShouldAdjustOtherScores(s));
                if(numTied > 1) {
                    int total = 0;
                    for (int i = 0; i< numTied; i++)
                    {
                        total += ((int)score.Place + i);
                    }
                    returnResults.CalculatedScores[score.Race].ScoreValue = (decimal)total / (decimal)numTied;
                }
            }

            return returnResults;
        }

        private bool ShouldAdjustOtherScores(Score score)
        {
            return !String.IsNullOrWhiteSpace(score.Code)
            && (GetScoreCode(score)?.AdjustOtherScores ?? true);
        }

        private void ClearRawScores(IEnumerable<Score> scores)
        {
            foreach(var score in scores)
            {
                if (!ShouldPreserveScore(score))
                {
                    score.Place = null;
                }
            }
        }

        private bool ShouldPreserveScore(Score score)
        {
            return String.IsNullOrWhiteSpace(score.Code)
                || ( GetScoreCode(score)?.PreserveResult ?? true);
        }

        private ScoreCode GetScoreCode(Score score)
        {
            return GetScoreCode(score.Code);
        }

        private ScoreCode GetScoreCode(string scoreCodeName)
        {
            if (String.IsNullOrWhiteSpace(scoreCodeName))
            {
                return null;
            }
            var returnScoreCode = _scoringSystem.ScoreCodes
                    .SingleOrDefault(c =>
                        c.Name.Equals(scoreCodeName, CASE_INSENSITIVE));

            if (returnScoreCode == null)
            {
                returnScoreCode = _scoringSystem.InheritedScoreCodes
                    .SingleOrDefault(c =>
                        c.Name.Equals(scoreCodeName, CASE_INSENSITIVE));
            }

            if (returnScoreCode == null)
            {
                returnScoreCode = GetScoreCode(DEFAULT_CODE);
            }
            return returnScoreCode;
        }
    }

}
