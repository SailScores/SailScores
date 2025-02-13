using SailScores.Api.Enumerations;
using SailScores.Core.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SailScores.Core.Scoring
{
    public abstract class BaseScoringCalculator : IScoringCalculator
    {
#pragma warning disable CA1707 // Identifiers should not contain underscores
        protected const string AVERAGE_FORMULANAME = "AVE";
        protected const string AVE_AFTER_DISCARDS_FORMULANAME = "AVE ND";
        protected const string AVE_PRIOR_RACES_FORMULANAME = "AVE P";
        protected const string SERIESCOMPETITORS_FORMULANAME = "SER+";
        protected const string MANUAL_FORMULANAME = "MAN";
        protected const string FINISHERSPLUS_FORMULANAME = "FIN+";
        protected const string PLACEPLUSPERCENT_FORMULANAME = "PLC%";
        protected const string CAMETOSTARTPLUS_FORMULANAME = "CTS+";
        protected const string FIXED_FORMULANAME = "FIX";
        protected const string TIE_FORMULANAME = "TIE";

        protected const string DNF_SCORENAME = "DNF";

        // Code to use if no result is found or if scorecode is not found in the system.
        //  This will be used if code defined in a child scoring system is used but the
        // series is scored with the ancestor
        protected const string DEFAULT_CODE = "DNC";

        protected const StringComparison CASE_INSENSITIVE = StringComparison.InvariantCultureIgnoreCase;

#pragma warning restore CA1707 // Identifiers should not contain underscores

        protected ScoringSystem ScoringSystem { get; set; }

        protected IComparer<SeriesCompetitorResults> CompetitorComparer { get; set; }

        protected BaseScoringCalculator(ScoringSystem scoringSystem)
        {
            ScoringSystem = scoringSystem;
            CompetitorComparer = new LowPointSeriesCompComparer();
        }

        public SeriesResults CalculateResults(Series series)
        {
            SeriesResults returnResults = GetResults(series);
            AddCodesUsed(returnResults);
            AddTrend(returnResults, series);
            return returnResults;
        }


        // This is the method that calls subclass methods for different types of scoring.
        // In other words, this is the heart of a scoring method.
        protected void SetScores(SeriesResults resultsWorkInProgress, IEnumerable<Score> scores)
        {
            ValidateSeries(resultsWorkInProgress, scores);
            ClearRawScores(scores);
            foreach (var comp in resultsWorkInProgress.Competitors)
            {
                // virtual and calls abstract GetBasicScore
                SeriesCompetitorResults compResults = CalculateSimpleScores(comp, scores);
                // the next three are private, so in BaseScoringCalculator
                AddDefaultScores(resultsWorkInProgress, compResults);
                CalculateRaceDependentScores(resultsWorkInProgress, compResults);
                CalculateSeriesDependentScores(resultsWorkInProgress, compResults);

                // these are virtual
                CalculateOverrides(resultsWorkInProgress, compResults);
                DiscardScores(resultsWorkInProgress, compResults);
                resultsWorkInProgress.Results[comp] = compResults;
            }
            // Next two are virtual
            CalculateTotals(resultsWorkInProgress, scores);
            CalculateRanks(resultsWorkInProgress);
            resultsWorkInProgress.Competitors = ReorderCompetitors(resultsWorkInProgress);
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
                if ((score.Race?.State ?? RaceState.Raced) != RaceState.Raced
                    && score.Race?.State != RaceState.Preliminary)
                {
                    continue;
                }
                returnResults.CalculatedScores[score.Race] = new CalculatedScore
                {
                    Discard = false,
                    RawScore = score,
                    ScoreValue = GetBasicScore(scores, score),
                    PerfectScoreValue = GetPerfectScore(scores, score),
                    CountsAsParticipation = CountsAsParticipation(score)
                };

            }

            return returnResults;
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

        protected virtual void CalculateTotals(
            SeriesResults results,
            IEnumerable<Score> scores)
        {
            foreach (var comp in results.Competitors)
            {
                var compResults = results.Results[comp];

                compResults.TotalScore = compResults
                    .CalculatedScores.Values
                    .Sum(s => !s.Discard ? (s.ScoreValue ?? 0.0m) : 0.0m);
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
                if (comp.TotalScore == null)
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

        protected static int GetNumberOfCompetitors(SeriesResults seriesResults)
        {
            return seriesResults.Competitors.Count;
        }

        protected virtual decimal? GetPenaltyScore(CalculatedScore score, Race race, ScoreCode scoreCode)
        {
            var dnfScore = GetDnfScore(race) ?? 1;
            var percentAdjustment = Convert.ToDecimal(scoreCode?.FormulaValue ?? 20);
            var percent = Math.Round(dnfScore * percentAdjustment / 100m, 1, MidpointRounding.AwayFromZero);

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

            return race.Scores
                       .Count(s => CountsAsStarted(s)) +
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
            if (IsSeriesBasedScore(defaultCode))
            {
                //Assume it's competitors plus for now. I suppose the default could be
                // the competitors average, but that would create all sort of problems, I think.
                return GetNumberOfCompetitors(resultsWorkInProgress) + (defaultCode.FormulaValue ?? 0);
            }
            return race.Scores.Count(s => CountsAsStarted(s)) +
                        defaultCode.FormulaValue;
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

        protected bool IsNonDiscardAverage(string code)
        {
            if (String.IsNullOrWhiteSpace(code))
            {
                return false;
            }
            var scoreCode = GetScoreCode(code);
            return scoreCode.Formula.Equals(AVE_AFTER_DISCARDS_FORMULANAME, CASE_INSENSITIVE);
        }

        protected bool CountsAsFinished(Score s)
        {
            if (s == null)
            {
                return false;
            }
            if (String.IsNullOrWhiteSpace(s.Code) &&
                (s.Place ?? 0) != 0)
            {
                return true;
            }
            var scoreCode = GetScoreCode(s);
            return CountsAsFinished(scoreCode);
        }

        protected bool CountsAsFinished(ScoreCode s)
        {
            return s.Finished ?? false;
        }

        protected bool CountsAsStarted(Score s)
        {
            if (s == null)
            {
                return false;
            }
            if (String.IsNullOrWhiteSpace(s.Code) &&
                (s.Place ?? 0) != 0)
            {
                return true;
            }
            var scoreCode = GetScoreCode(s);
            return CountsAsStarted(scoreCode);
        }

        protected bool CountsAsStarted(ScoreCode s)
        {
            return s.Started ?? false;
        }

        protected bool CountsAsParticipation(Score s)
        {
            if (s == null)
            {
                return false;
            }
            if (String.IsNullOrWhiteSpace(s.Code) &&
                (s.Place ?? 0) != 0)
            {
                return true;
            }
            var scoreCode = GetScoreCode(s);
            return CountsAsParticipation(scoreCode);
        }

        protected bool CountsAsParticipation(ScoreCode s)
        {
            return (s.CameToStart ?? false) || (s.CountAsParticipation ?? false);
        }

        protected bool CameToStart(Score s)
        {
            if (String.IsNullOrWhiteSpace(s.Code) &&
                (s.Place ?? 0) != 0)
            {
                return true;
            }
            var scoreCode = GetScoreCode(s);
            return CameToStart(scoreCode);
        }

        protected bool CameToStart(ScoreCode s)
        {
            return s.CameToStart ?? false;
        }


        protected int GetNumberOfDiscards(SeriesResults resultsWorkInProgress)
        {
            return GetNumberOfDiscards(resultsWorkInProgress.GetSailedRaceCount());
        }

        protected int GetNumberOfDiscards(int numberOfRaces)
        {
            if (numberOfRaces == 0)
            {
                return 0;
            }
            string[] discardStrings;
            if (String.IsNullOrWhiteSpace(ScoringSystem?.DiscardPattern))
            {
                discardStrings = new string[] { "0" };
            }
            else
            {
                discardStrings = ScoringSystem.DiscardPattern.Split(',');
            }
            string selectedString;
            if (numberOfRaces > discardStrings.Length)
            {
                selectedString = discardStrings[^1];
            }
            else
            {
                selectedString = discardStrings[numberOfRaces - 1];
            }

            if (int.TryParse(selectedString, out int returnValue))
            {
                return returnValue;
            }

            if (numberOfRaces == 1)
            {
                return 0;
            }
            return GetNumberOfDiscards(numberOfRaces - 1);

        }

        // Ensure consistency of submitted results for calculations.
        protected void ValidateSeries(SeriesResults results, IEnumerable<Score> scores)
        {
            bool allRacesFound = scores.All(s => results.Races.Any(
                r => r.Id == s.RaceId
                    || r == s.Race));
            bool allCompetitorsFound = scores.All(s => results.Competitors.Any(
                c => c.Id == s.CompetitorId
                    || c == s.Competitor));

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


        protected abstract Decimal? GetBasicScore(
            IEnumerable<Score> allScores,
            Score currentScore);
        
        protected abstract Decimal? GetPerfectScore(
            IEnumerable<Score> allScores,
            Score currentScore);

        protected bool ShouldAdjustOtherScores(Score score)
        {
            return !String.IsNullOrWhiteSpace(score.Code)
            && (GetScoreCode(score)?.AdjustOtherScores ?? true);
        }


        protected bool ShouldPreserveScore(Score score)
        {
            return String.IsNullOrWhiteSpace(score.Code)
                || (GetScoreCode(score)?.PreserveResult ?? true);
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
            var returnScoreCode = ScoringSystem.ScoreCodes
                    .SingleOrDefault(c =>
                        c.Name.Equals(scoreCodeName, CASE_INSENSITIVE));

            if (returnScoreCode == null)
            {
                returnScoreCode = ScoringSystem.InheritedScoreCodes
                    ?.SingleOrDefault(c =>
                        c.Name.Equals(scoreCodeName, CASE_INSENSITIVE));
            }

            if (returnScoreCode == null)
            {
                returnScoreCode = GetScoreCode(DEFAULT_CODE);
                if (returnScoreCode == null)
                {
                    returnScoreCode = new ScoreCode
                    {
                        Id = Guid.NewGuid(),
                        Name = "Default",
                        Formula = "FIN+",
                        FormulaValue = 2,
                        CameToStart = false,
                        Finished = false,
                        Discardable = true
                    };
                }
            }
            return returnScoreCode;
        }

        private static IList<Competitor> ReorderCompetitors(SeriesResults results)
        {
            return results.Competitors.OrderBy(c => results.Results[c].Rank ?? int.MaxValue).ToList();
        }

        private void AddCodesUsed(SeriesResults results)
        {
            var scoreCodes = new Dictionary<string, ScoreCodeSummary>();

            foreach (var comp in results.Competitors)
                foreach (var race in results.SailedRaces)
                {
                    var curScore = results.Results[comp].CalculatedScores[race];
                    if (!String.IsNullOrWhiteSpace(curScore.RawScore.Code)
                        && !scoreCodes.ContainsKey(curScore.RawScore.Code))
                    {
                        scoreCodes.Add(curScore.RawScore.Code,
                            GetScoreCodeSummary(curScore.RawScore.Code));
                    }
                }

            results.ScoreCodesUsed = scoreCodes;
        }

        protected virtual ScoreCodeSummary GetScoreCodeSummary(string code)
        {
            var codeDef = GetScoreCode(code);

            return new ScoreCodeSummary
            {
                Name = codeDef.Name,
                Description = codeDef.Description,
                Formula = GetFormulaString(codeDef)
            };

        }

        private string GetFormulaString(ScoreCode codeDef)
        {
            var returnString = new StringBuilder();
            switch (codeDef.Formula.ToUpperInvariant())
            {
                case "COD":
                    returnString.Append($"Scored like {codeDef.ScoreLike}");
                    break;
                case "FIN+":
                    returnString.Append("Number of boats that finished race");
                    returnString.Append(GetNumberIfExists(codeDef));
                    break;
                case "SER+":
                    returnString.Append("Number of boats entered in series");
                    returnString.Append(GetNumberIfExists(codeDef));
                    break;
                case "CTS+":
                    returnString.Append("Number of boats that came to starting area");
                    returnString.Append(GetNumberIfExists(codeDef));
                    break;
                case "AVE":
                    returnString.Append("Average of results");
                    break;
                case "AVE ND":
                    returnString.Append("Average of non-discarded results");
                    break;
                case "AVE P":
                    returnString.Append("Average of results in prior races");
                    break;
                case "PLC%":
                    returnString.Append($"Place + penalty ({codeDef.FormulaValue}% of DNF score)");
                    break;
                case "MAN":
                    returnString.Append($"Manually entered value");
                    break;
                case "FIX":
                    returnString.Append($"Fixed at {codeDef.FormulaValue}");
                    break;
                case "TIE":
                    returnString.Append($"Average of tied places");
                    break;
            }

            if (!(codeDef.Discardable ?? true))
            {
                returnString.Append("; not excludable");
            }
            return returnString.ToString();
        }

        private string GetNumberIfExists(ScoreCode codeDef)
        {

            if ((codeDef.FormulaValue ?? 0) == 0)
            {
                return string.Empty;
            }

            return $" + {codeDef.FormulaValue}";
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
            if ((series.TrendOption ?? TrendOption.None) == TrendOption.None)
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
            if (!newSeries.Races.Where(r => (r.State == RaceState.Raced || r.State == null)).Any())
            {
                return;
            }

            var oldResults = GetResults(newSeries);
            int maxOldRank = oldResults.Results.Values.Max(v => v.Rank) ?? 0;
            foreach (var comp in results.Competitors)
            {
                int oldRank;

                if (oldResults.Results.ContainsKey(comp)
                    && oldResults.Results[comp]?.Rank != null)
                {
                    oldRank = oldResults.Results[comp].Rank.Value;
                }
                else
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
            var lastRaceDate = races.Where(r => (r.State ?? RaceState.Raced) == RaceState.Raced
                || r.State == RaceState.Preliminary)
                .Max(r => r.Date) ?? DateTime.Today;
            return races.Where(r => r.Date == null || r.Date <= lastRaceDate.AddDays(-1)).ToList();
        }

        private IList<Race> RemoveLastWeeksRaces(IList<Race> races)
        {
            var lastRaceDate = races.Where(r => (r.State ?? RaceState.Raced) == RaceState.Raced
                || r.State == RaceState.Preliminary)
                .Max(r => r.Date) ?? DateTime.Today;
            return races.Where(r => r.Date == null || r.Date <= lastRaceDate.AddDays(-7)).ToList();
        }
        private IList<Race> RemoveLastRace(IList<Race> races)
        {
            if (races.Count <= 1)
            {
                return new List<Race>();
            }
            var lastRaceId = races
                .Where(r => (r.State ?? RaceState.Raced) == RaceState.Raced
                    || r.State == RaceState.Preliminary)
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

        protected virtual void CalculateRaceDependentScores(SeriesResults resultsWorkInProgress, SeriesCompetitorResults compResults)
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
                    }
                    else if (IsRaceBasedValue(scoreCode))
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
            // first pass to make sure series based DNC's are filled in.
            foreach (var race in resultsWorkInProgress.SailedRaces)
            {
                var score = compResults.CalculatedScores[race];
                var scoreCode = GetScoreCode(score.RawScore);
                if (IsSeriesBasedScore(scoreCode) && !IsAverage(score.RawScore.Code))
                {
                    score.ScoreValue = CalculateSeriesBasedValue(
                        resultsWorkInProgress,
                        compResults,
                        race);
                }
            }

            // second pass to get averages.
            foreach (var race in resultsWorkInProgress.SailedRaces)
            {
                var score = compResults.CalculatedScores[race];
                var scoreCode = GetScoreCode(score.RawScore);
                if (IsSeriesBasedScore(scoreCode) && IsAverage(score.RawScore.Code))
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
            return (scoreCode.Formula.ToUpperInvariant()) switch
            {
                AVERAGE_FORMULANAME => CalculateAverage(compResults),
                AVE_AFTER_DISCARDS_FORMULANAME => CalculateAverageNoDiscards(compResults),
                AVE_PRIOR_RACES_FORMULANAME => CalculateAverageOfPrior(compResults, race),
                SERIESCOMPETITORS_FORMULANAME => GetNumberOfCompetitors(resultsWorkInProgress) + (scoreCode.FormulaValue ?? 0),
                _ => null,
            };
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

        protected static bool IsTrivialCalculation(ScoreCode scoreCode)
        {
            return scoreCode.Formula.Equals(MANUAL_FORMULANAME, CASE_INSENSITIVE)
                || scoreCode.Formula.Equals(FIXED_FORMULANAME, CASE_INSENSITIVE);
        }

        protected decimal? GetTrivialScoreValue(CalculatedScore score)
        {
            var scoreCode = GetScoreCode(score.RawScore);
            switch (scoreCode.Formula)
            {
                case FIXED_FORMULANAME:
                    return scoreCode.FormulaValue;
                case MANUAL_FORMULANAME:
                    if (score.RawScore.CodePoints.HasValue)
                    {
                        return score.RawScore.CodePoints;
                    }
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
            return (scoreCode.Formula.ToUpperInvariant()) switch
            {
                FINISHERSPLUS_FORMULANAME =>
                    race.Scores.Count(s => CountsAsFinished(s)) +
                        scoreCode.FormulaValue,
                CAMETOSTARTPLUS_FORMULANAME =>
                    race.Scores.Count(s => CameToStart(s)) +
                        scoreCode.FormulaValue,
                PLACEPLUSPERCENT_FORMULANAME =>
                    GetPenaltyScore(score, race, scoreCode),
                _ =>
                    throw new InvalidOperationException(
                        "Score code definition issue with race based score code."),
            };
        }

        private void ClearRawScores(IEnumerable<Score> scores)
        {
            foreach (var score in scores)
            {
                if (!ShouldPreserveScore(score))
                {
                    score.Place = null;
                }
            }
        }


        private decimal? CalculateAverage(
            SeriesCompetitorResults compResults)
        {
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
            int numNoDiscardAverages = compResults.CalculatedScores
                .Values.Count(s =>
                    IsNonDiscardAverage(s.RawScore.Code));
            int discards = GetNumberOfDiscards(compResults.CalculatedScores.Count);

            decimal average;
            if (compResults.CalculatedScores.Count - discards <= numNoDiscardAverages)
            {
                average = compResults.CalculatedScores.Values
                    .Where(s => (s.ScoreValue ?? 0m) != 0m && !IsAverage(s.RawScore.Code))
                    .OrderBy(s => s.ScoreValue)
                    .FirstOrDefault()?.ScoreValue ?? 0m;
            }
            else
            {
                average = compResults.CalculatedScores.Values
                   .Where(s => (s.ScoreValue ?? 0m) != 0m && !IsAverage(s.RawScore.Code))
                   .OrderBy(s => s.ScoreValue)
                   .Take(compResults.CalculatedScores.Count - numAverages - discards)
                   .Average(s => s.ScoreValue) ?? 0m;
            }
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

            var average = resultsToUse.Values
                .Where(s => (s.ScoreValue ?? 0m) != 0m && !IsAverage(s.RawScore.Code))
                .Average(s => s.ScoreValue) ?? 0m;

            return Math.Round(average, 1, MidpointRounding.AwayFromZero);

        }
    }

}
