﻿using SailScores.Api.Enumerations;
using SailScores.Core.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SailScores.Core.Scoring
{
    public abstract class BaseScoringCalculator : IScoringCalculator
    {
        protected const string AVERAGE_FORMULANAME = "AVE";
        protected const string AVE_AFTER_DISCARDS_FORMULANAME = "AVE ND";
        protected const string AVE_PRIOR_RACES_FORMULANAME = "AVE P";
        protected const string SERIESCOMPETITORS_FORMULANAME = "SER+";
        protected const string MANUAL_FORMULANAME = "MAN";
        protected const string FINISHERSPLUS_FORMULANAME = "FIN+";
        protected const string PLACEPLUSPERCENT_FORMULANAME = "PLC%";
        protected const string CAMETOSTARTPLUS_FORMULANAME = "CTS+";
        protected const string FIXED_FORMULANAME = "FIX";

        protected const string DNF_SCORENAME = "DNF";

        // Code to use if no result is found or if scorecode is not found in the system.
        //  This will be used if code defined in a child scoring system is used but the
        // series is scored with the ancestor
        protected readonly string DEFAULT_CODE = "DNC";

        protected const StringComparison CASE_INSENSITIVE = StringComparison.InvariantCultureIgnoreCase;


        protected readonly ScoringSystem _scoringSystem;

        protected IComparer<SeriesCompetitorResults> CompetitorComparer;

        protected BaseScoringCalculator(ScoringSystem scoringSystem)
        {
            _scoringSystem = scoringSystem;
            CompetitorComparer = new LowPointSeriesCompComparer();
        }

        public SeriesResults CalculateResults(Series series)
        {
            SeriesResults returnResults = GetResults(series);
            AddTrend(returnResults,series);
            return returnResults;
        }

        private SeriesResults GetResults(Series series)
        {
            SeriesResults returnResults = BuildResults(series);

            SetScores(returnResults,
                series
                .Races
                .SelectMany(
                    r => r
                        .Scores));
            return returnResults;
        }

        private void AddTrend(SeriesResults results, Series series)
        {
            if((series.TrendOption ?? TrendOption.None) == TrendOption.None)
            {
                return;
            }
            var newSeries = series.ShallowCopy();
            switch (series.TrendOption)
            {
                case TrendOption.PreviousDay:
                    newSeries.Races = RemoveLastDaysRaces(series.Races);
                    break;
                case TrendOption.PreviousRace:
                    newSeries.Races = RemoveLastRace(series.Races);
                    break;
                case TrendOption.PreviousWeek:
                    newSeries.Races = RemoveLastWeeksRaces(series.Races);
                    break;
            }

            var oldResults = GetResults(newSeries);
            int maxOldRank = oldResults.Results.Values.Max(v => v.Rank) ?? 0;
            foreach(var comp in results.Competitors)
            {
                int oldRank;

                if(oldResults.Results.ContainsKey(comp)
                    && oldResults.Results[comp]?.Rank != null)
                {
                    oldRank = oldResults.Results[comp].Rank.Value;
                } else
                {
                    oldRank = maxOldRank + 1;
                }
                if (results.Results[comp]?.Rank != null)
                {
                    results.Results[comp].Trend =
                       0 - (results.Results[comp].Rank -
                       oldRank);
                }
            }
        }

        private IList<Race> RemoveLastDaysRaces(IList<Race> races)
        {
            var lastRaceDate = races.Where(r => (r.State ?? RaceState.Raced) == RaceState.Raced)
                .Max(r => r.Date) ?? DateTime.Today;
            return races.Where(r => r.Date == null || r.Date < lastRaceDate.AddDays(-1)).ToList();
        }

        private IList<Race> RemoveLastWeeksRaces(IList<Race> races)
        {
            var lastRaceDate = races.Where(r => (r.State ?? RaceState.Raced) == RaceState.Raced)
                .Max(r => r.Date) ?? DateTime.Today;
            return races.Where(r => r.Date == null || r.Date < lastRaceDate.AddDays(-7)).ToList();
        }
        private IList<Race> RemoveLastRace(IList<Race> races)
        {
            if(races.Count <= 1)
            {
                return new List<Race>();
            }
            var lastRaceId = races
                .Where(r => (r.State ?? RaceState.Raced) == RaceState.Raced)
                .OrderBy(r => r.Date).ThenBy(r => r.Order).Last().Id;
            return races.Where(r => r.Id != lastRaceId).ToList();
        }

        private SeriesResults BuildResults(Series series)
        {
            var returnResults = new SeriesResults
            {
                Races = series.Races
                    .OrderBy(r => r.Date)
                    .ThenBy(r => r.Order).ToList(),
                Competitors = series
                    .Races
                    .SelectMany(
                        r => r
                            .Scores
                            .Select(s => s.Competitor))
                    .Distinct()
                    .ToList(),
                Results = new Dictionary<Competitor, SeriesCompetitorResults>()
            };

            returnResults.NumberOfDiscards = GetNumberOfDiscards(returnResults);
            return returnResults;
        }

        protected void SetScores(SeriesResults resultsWorkInProgress, IEnumerable<Score> scores)
        {
            ValidateSeries(resultsWorkInProgress, scores);
            ClearRawScores(scores);
            foreach (var comp in resultsWorkInProgress.Competitors)
            {
                SeriesCompetitorResults compResults = CalculateSimpleScores(comp, scores);
                AddDefaultScores(resultsWorkInProgress, compResults);
                CalculateRaceDependentScores(resultsWorkInProgress, compResults);
                CalculateSeriesDependentScores(resultsWorkInProgress, compResults);
                CalculateOverrides(resultsWorkInProgress, compResults);
                DiscardScores(resultsWorkInProgress, compResults);
                resultsWorkInProgress.Results[comp] = compResults;
            }
            CalculateTotals(resultsWorkInProgress, scores);
            CalculateRanks(resultsWorkInProgress);
            resultsWorkInProgress.Competitors = ReorderCompetitors(resultsWorkInProgress);
        }

        private void AddDefaultScores(
            SeriesResults resultsWorkInProgress,
            SeriesCompetitorResults compResults)
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

        private void CalculateSeriesDependentScores(
            SeriesResults resultsWorkInProgress,
            SeriesCompetitorResults compResults)
        {
            foreach (var race in resultsWorkInProgress.SailedRaces)
            {
                var score = compResults.CalculatedScores[race];
                var scoreCode = GetScoreCode(score.RawScore);
                if (score != null && IsSeriesBasedScore(scoreCode))
                {
                    score.ScoreValue = CalculateSeriesBasedValue(
                        resultsWorkInProgress,
                        compResults,
                        race);
                }
            }
        }

        private Decimal? CalculateSeriesBasedValue(
            SeriesResults resultsWorkInProgress,
            SeriesCompetitorResults compResults,
            Race race)
        {
            var score = compResults.CalculatedScores[race];
            var scoreCode = GetScoreCode(score.RawScore);
            switch (scoreCode.Formula.ToUpperInvariant())
            {
                case AVERAGE_FORMULANAME:
                    return CalculateAverage(compResults);
                case AVE_AFTER_DISCARDS_FORMULANAME:
                    return CalculateAverageNoDiscards(compResults);
                case AVE_PRIOR_RACES_FORMULANAME:
                    return CalculateAverageOfPrior(compResults, race);
                case SERIESCOMPETITORS_FORMULANAME:
                    return GetNumberOfCompetitors(resultsWorkInProgress) + (scoreCode.FormulaValue ?? 0);
            }
            return null;
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

        protected virtual void CalculateOverrides(
            SeriesResults resultsWorkInProgress,
            SeriesCompetitorResults compResults)
        {
            foreach (var race in resultsWorkInProgress.SailedRaces)
            {
                var score = compResults.CalculatedScores[race];
                var defaultScore = GetDefaultScore(race, resultsWorkInProgress);
                if (score?.ScoreValue != null && score.ScoreValue > defaultScore)
                {
                    score.ScoreValue = defaultScore;
                }
            }
        }


        protected virtual void CalculateRanks(SeriesResults resultsWorkInProgress)
        {
            var orderedComps = resultsWorkInProgress.Results.Values
                .OrderBy(s => s, CompetitorComparer);

            int i = 1;
            SeriesCompetitorResults prevComp = null;
            foreach (var comp in orderedComps)
            {
                if(comp.TotalScore == null)
                {
                    comp.Rank = null;
                }
                else if (prevComp != null && CompetitorComparer.Compare(comp, prevComp) == 0)
                {
                    comp.Rank = prevComp.Rank;
                }
                else
                {
                    comp.Rank = i;
                }
                i++;
                prevComp = comp;
            }
        }

        private IList<Competitor> ReorderCompetitors(SeriesResults results)
        {
            return results.Competitors.OrderBy(c => results.Results[c].Rank ?? int.MaxValue).ToList();
        }

        protected int GetNumberOfCompetitors(SeriesResults seriesResults)
        {
            return seriesResults.Competitors.Count();
        }


        private bool IsTrivialCalculation(ScoreCode scoreCode)
        {
            return scoreCode.Formula.Equals(MANUAL_FORMULANAME, CASE_INSENSITIVE)
                || scoreCode.Formula.Equals(FIXED_FORMULANAME,CASE_INSENSITIVE);
        }

        private decimal? GetTrivialScoreValue(CalculatedScore score)
        {
            var scoreCode = GetScoreCode(score.RawScore);
            switch (scoreCode.Formula)
            {
                case FIXED_FORMULANAME:
                    return scoreCode.FormulaValue;
                case MANUAL_FORMULANAME:
                    if (score.RawScore.Place.HasValue)
                    {
                        return Convert.ToDecimal(score.RawScore.Place);
                    }
                    break;
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

        protected virtual decimal? GetPenaltyScore(CalculatedScore score, Race race, ScoreCode scoreCode)
        {
            var dnfScore = GetDnfScore(race) ?? 1;
            var percentAdjustment = Convert.ToDecimal(scoreCode?.FormulaValue ?? 20);
            var percent = Math.Round(dnfScore * percentAdjustment / 100m, MidpointRounding.AwayFromZero);

            return Math.Min(dnfScore, percent + (score.ScoreValue ?? score.RawScore.Place ?? 0));
        }

        protected decimal? GetDnfScore(Race race)
        {
            var dnfCode = GetScoreCode(DNF_SCORENAME);
            if (IsTrivialCalculation(dnfCode))
            {
                return GetTrivialScoreValue(new CalculatedScore
                {
                    RawScore = new Score { Code = dnfCode.Name }
                });
            }
            if (IsRaceBasedValue(dnfCode))
            {
                return CalculateRaceBasedValue(new CalculatedScore
                {
                    RawScore = new Score { Code = dnfCode.Name }
                }, race);
            }

            return race.Scores.Where(s => CountsAsStarted(s)).Count() +
                        dnfCode.FormulaValue;
        }


        protected virtual decimal? GetDefaultScore(Race race, SeriesResults resultsWorkInProgress)
        {
            var defaultCode = GetScoreCode(DEFAULT_CODE);
            if (IsTrivialCalculation(defaultCode))
            {
                return GetTrivialScoreValue(new CalculatedScore
                {
                    RawScore = new Score { Code = defaultCode.Name }
                });
            }
            if (IsRaceBasedValue(defaultCode))
            {
                return CalculateRaceBasedValue(new CalculatedScore
                {
                    RawScore = new Score { Code = defaultCode.Name }
                }, race);
            }
            if(IsSeriesBasedScore(defaultCode))
            {
                //Assume it's competitors plus for now. I suppose the default could be
                // the competitors average, but that would create all sort of problems, I think.
                return GetNumberOfCompetitors(resultsWorkInProgress) + (defaultCode.FormulaValue ?? 0);
            }
            return race.Scores.Where(s => CountsAsStarted(s)).Count() +
                        defaultCode.FormulaValue;
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

        protected bool IsAverage(string code)
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

        protected bool CountsAsStarted(Score s)
        {
            if (String.IsNullOrWhiteSpace(s.Code) &&
                (s.Place ?? 0) != 0)
            {
                return true;
            }
            var scoreCode = GetScoreCode(s);
            return scoreCode.Started ?? false;
        }

        protected bool CameToStart(Score s)
        {
            if (String.IsNullOrWhiteSpace(s.Code) &&
                (s.Place ?? 0) != 0)
            {
                return true;
            }
            var scoreCode = GetScoreCode(s);
            return scoreCode.CameToStart ?? false;
        }

        protected virtual void CalculateTotals(
            SeriesResults results,
            IEnumerable<Score> scores)
        {
            foreach(var comp in results.Competitors)
            {
                var compResults = results.Results[comp];

                compResults.TotalScore = compResults
                    .CalculatedScores.Values
                    .Sum(s => !s.Discard ? (s.ScoreValue ?? 0.0m) : 0.0m);
            }
        }

        protected virtual void DiscardScores(
            SeriesResults resultsWorkInProgress,
            SeriesCompetitorResults compResults)
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

        protected int GetNumberOfDiscards(SeriesResults resultsWorkInProgress)
        {
            return GetNumberOfDiscards(resultsWorkInProgress.GetSailedRaceCount());
        }

        protected int GetNumberOfDiscards(int numberOfRaces)
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


        protected void ValidateSeries(SeriesResults results, IEnumerable<Score> scores)
        {
            bool allRacesFound = scores.All(s => results.Races.Any(
                r => r.Id == s.RaceId
                    || r == s.Race));
            bool allCompetitorsFound = scores.All(s => results.Competitors.Any(
                c => c.Id == s.CompetitorId
                    || c == s.Competitor ));

            //Used to check and make sure all score codes were found. but no more.

            if (!allRacesFound)
            {
                throw new InvalidOperationException(
                    "A score for a race that is not in the series was provided to SeriesCalculator");
            }

            if (!allCompetitorsFound)
            {
                throw new InvalidOperationException(
                    "A score for a competitor that is not in the series was provided to SeriesCalculator");
            }
        }

        protected virtual SeriesCompetitorResults CalculateSimpleScores(Competitor comp, IEnumerable<Score> scores)
        {
            var returnResults = new SeriesCompetitorResults
            {
                Competitor = comp,
                CalculatedScores = new Dictionary<Race, CalculatedScore>()
            };
            foreach (var score in scores.Where(s => s.Competitor == comp))
            {
                if ((score.Race?.State ?? RaceState.Raced) != RaceState.Raced)
                {
                    continue;
                }
                returnResults.CalculatedScores[score.Race] = new CalculatedScore
                {
                    Discard = false,
                    RawScore = score
                };
                returnResults.CalculatedScores[score.Race].ScoreValue = 
                    GetBasicScore(scores, score);

            }

            return returnResults;
        }

        protected abstract Decimal? GetBasicScore(
            IEnumerable<Score> allScores,
            Score currentScore);

        protected bool ShouldAdjustOtherScores(Score score)
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

        protected bool ShouldPreserveScore(Score score)
        {
            return String.IsNullOrWhiteSpace(score.Code)
                || ( GetScoreCode(score)?.PreserveResult ?? true);
        }

        protected ScoreCode GetScoreCode(Score score)
        {
            return GetScoreCode(score.Code);
        }

        protected ScoreCode GetScoreCode(string scoreCodeName)
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