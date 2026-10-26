using SailScores.Api.Enumerations;
using SailScores.Core.Model;
using SailScores.Core.Scoring.Formulas;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SailScores.Core.Scoring
{
    // Scores a series with a ScoringRecipe. Every scoring system scores through this class;
    // what differs between systems is in its recipe (Scoring/Systems/).
    //
    // Each run first copies the series' scores into its workspace, then applies any input
    // steps (handicap time correction) to the copies, and clears the place of each code that
    // doesn't keep it. The series passed in is never changed.
    //
    // For each competitor (ScoreCompetitor):
    //   1. Race points        recipe.RacePoints, for every finish
    //   2. Code limits        score code group overages become the overage code
    //   3. Missing results    a race with no score is scored as recipe.DefaultCode (DNC)
    //   4. Race formulas      FIX, MAN, FIN+, CTS+, STR+, PLC% (or recipe.CodedResults instead)
    //   5. Series formulas    SER+, then the averages (unless recipe.SeriesFormulas is off)
    //   6. Child scores       a summary series' preset scores are used as they are
    //   7. Adjustments        recipe.Adjustments, in order
    //   8. Discards           recipe.Discards, in order
    // Then for the series (SetScores):
    //   9. Totals             recipe.Total
    //   10. Eligibility       recipe.Eligibility, in order
    //   11. Ranks             recipe.Ranking
    //   12. Finish            each score's place and code as shown, the code glossary
    //                         (recipe.Glossary), then the trend
    //
    // Each run (results, the trend's rerun, an absent competitor's score) starts a new
    // ScoringWorkspace. Rules get it as an argument; this class keeps the current one while it
    // scores, because IScoringHost's answers (fleet place mode, discard method) depend on it.
    public sealed class ScoringPipeline : IScoringCalculator, IScoringHost
    {
#pragma warning disable CA1707 // Identifiers should not contain underscores
        private const string DNF_SCORENAME = "DNF";

        // Code to use if no result is found or if scorecode is not found in the system.
        //  This will be used if code defined in a child scoring system is used but the
        // series is scored with the ancestor. DNC unless the recipe says otherwise.
        private string DEFAULT_CODE => Recipe.DefaultCode;

        private const StringComparison CASE_INSENSITIVE = StringComparison.InvariantCultureIgnoreCase;

#pragma warning restore CA1707 // Identifiers should not contain underscores

        private readonly IComparer<SeriesCompetitorResults> _competitorComparer;

        private readonly IReadOnlyList<IScoreInputStep> _inputSteps;

        public ScoringPipeline(ScoringSystem scoringSystem, ScoringRecipe recipe)
            : this(scoringSystem, recipe, [])
        {
        }

        private ScoringPipeline(ScoringSystem scoringSystem, ScoringRecipe recipe,
            IReadOnlyList<IScoreInputStep> inputSteps)
        {
            ScoringSystem = scoringSystem;
            Recipe = recipe;
            _inputSteps = inputSteps;
            _competitorComparer = Comparer<SeriesCompetitorResults>.Create(
                (x, y) => recipe.Ranking.Compare(Workspace, x, y));
        }

        // The same system with a step that changes the scores' copies before they are
        // scored (handicap time correction).
        public ScoringPipeline WithInputStep(IScoreInputStep step) =>
            new(ScoringSystem, Recipe, [.. _inputSteps, step]);

        public ScoringRecipe Recipe { get; }

        private ScoringSystem ScoringSystem { get; }

        private FormulaSet Formulas => Recipe.Formulas;

        // The current run's workspace.
        private ScoringWorkspace Workspace { get; set; }

        // True when places come from the whole race (no fleet, or full race scores).
        private bool UseOriginalPlace => Workspace?.UseOriginalPlace ?? false;

        public override string ToString() => Recipe.Name;

        public SeriesResults CalculateResults(Series series)
        {
            SeriesResults returnResults = GetResults(series);
            AddCodesUsed(returnResults);
            AddTrend(returnResults, series);
            return returnResults;
        }

        private void SetScores(SeriesResults resultsWorkInProgress, IEnumerable<Score> scores)
        {
            ValidateSeries(resultsWorkInProgress, scores);
            ClearPlacesNotKept(scores);
            foreach (var comp in resultsWorkInProgress.Competitors)
            {
                resultsWorkInProgress.Results[comp] = ScoreCompetitor(resultsWorkInProgress, comp, scores);
            }


            if (DiscardMethod == DiscardMethod.PreserveChildSeriesDiscards)
            {
                // Not known until each competitor's child discards are applied.
                resultsWorkInProgress.NumberOfDiscards = resultsWorkInProgress.Results.Values
                    .Select(r => r.CalculatedScores.Values.Count(s => s.Discard))
                    .DefaultIfEmpty(0)
                    .Max();
            }

            CalculateTotals(resultsWorkInProgress, scores);
            CalculateRanks(resultsWorkInProgress);
            resultsWorkInProgress.Competitors = ReorderCompetitors(resultsWorkInProgress);
        }
        
        private SeriesCompetitorResults ScoreCompetitor(
            SeriesResults resultsWorkInProgress,
            Competitor comp,
            IEnumerable<Score> scores)
        {
            // 1. Race points
            SeriesCompetitorResults compResults = CalculateSimpleScores(comp, scores);

            // 2. Code limits, using the expected number of discards (not actual).
            ApplyScoreCodeGroupLimits(resultsWorkInProgress, compResults);

            // 3. Missing results
            AddDefaultScores(resultsWorkInProgress, compResults);
            // 4. Race formulas
            CalculateRaceDependentScores(resultsWorkInProgress, compResults);
            // 5. Series formulas
            CalculateSeriesDependentScores(resultsWorkInProgress, compResults);
            // 6. Child scores: used as-is, whatever their code would give.
            ApplyPresetScores(compResults);

            // 7. Adjustments
            foreach (var adjustment in Recipe.Adjustments)
            {
                adjustment.Apply(Workspace, resultsWorkInProgress, compResults);
            }
            // 8. Discards
            foreach (var discards in Recipe.Discards)
            {
                discards.Apply(Workspace, resultsWorkInProgress, compResults);
            }
            return compResults;
        }

        // The series score of a competitor who missed every race (TotalScore, else Average,
        // else PointsEarned), scored against this series' competitors without being counted
        // as one of them. Null when the scoring system gives such a competitor no score.
        public decimal? CalculateAbsentCompetitorScore(Series series)
        {
            StartRun(series);
            var results = GatherInitialResults(series);
            var scores = GetScoresForCalculation(series).ToList();
            ValidateSeries(results, scores);
            ClearPlacesNotKept(scores);

            var absent = new Competitor { Id = Guid.NewGuid(), Name = "Absent" };
            var absentResults = new SeriesResults
            {
                Races = results.Races,
                Competitors = new List<Competitor> { absent },
                NumberOfDiscards = results.NumberOfDiscards,
                DiscardMethod = results.DiscardMethod,
                Results = new Dictionary<Competitor, SeriesCompetitorResults>
                {
                    [absent] = ScoreCompetitor(results, absent, scores)
                }
            };
            CalculateTotals(absentResults, scores);
            var compResults = absentResults.Results[absent];
            return compResults.TotalScore ?? compResults.Average ?? compResults.PointsEarned;
        }

        private SeriesCompetitorResults CalculateSimpleScores(Competitor comp, IEnumerable<Score> scores)
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
                    ScoreValue = score.PresetScoreValue ?? GetBasicScore(scores, score),
                    PerfectScoreValue = GetPerfectScore(scores, score),
                    CountsAsParticipation = CountsAsParticipation(score)
                };

            }

            return returnResults;
        }

        private static void ApplyPresetScores(SeriesCompetitorResults compResults)
        {
            foreach (var score in compResults.CalculatedScores.Values)
            {
                if (score.RawScore?.PresetScoreValue is decimal preset)
                {
                    score.ScoreValue = preset;
                }
            }
        }

        // The method used for the series being scored.
        private DiscardMethod DiscardMethod =>
            Workspace?.DiscardMethod ?? ResolveDiscardMethod(null);

        private void StartRun(Series series)
        {
            _scoreCodeCache.Clear();
            Workspace = new ScoringWorkspace
            {
                System = ScoringSystem,
                Formulas = Formulas,
                Host = this,
                DiscardMethod = ResolveDiscardMethod(series),
                // Original race places when there is no fleet filter (standard scoring), or
                // when the series keeps full-race positions.
                UseOriginalPlace = series.FleetId == null || series.UseFullRaceScores == true,
                UseFullRaceScores = series.FleetId != null && series.UseFullRaceScores == true,
                RaceScores = CopyScores(series.Races)
            };
            foreach (var step in _inputSteps)
            {
                step.Apply(Workspace, series.Races);
            }
        }

        // A working copy of each race's scores. A score listed in two races is copied once.
        private static Dictionary<Race, IReadOnlyList<Score>> CopyScores(IEnumerable<Race> races)
        {
            var copies = new Dictionary<Score, Score>(ReferenceEqualityComparer.Instance);
            var scoresByRace = new Dictionary<Race, IReadOnlyList<Score>>(ReferenceEqualityComparer.Instance);
            foreach (var race in races)
            {
                scoresByRace[race] = (race.Scores ?? Enumerable.Empty<Score>())
                    .Select(score =>
                    {
                        if (!copies.TryGetValue(score, out var copy))
                        {
                            copy = score.ShallowCopy();
                            copies[score] = copy;
                        }
                        return copy;
                    })
                    .ToList();
            }
            return scoresByRace;
        }

        // ChildSeriesDiscardsOrPattern becomes PreserveChildSeriesDiscards when the races
        // carry child discards (a summary series using its child series' races), and
        // RaceCountPattern otherwise.
        private DiscardMethod ResolveDiscardMethod(Series series)
        {
            var method = ScoringSystem?.EffectiveDiscardMethod ?? DiscardMethod.RaceCountPattern;
            if (method != DiscardMethod.ChildSeriesDiscardsOrPattern)
            {
                return method;
            }
            return series?.Races?.Any(r => r?.CompetitorIdsDiscardedInChild != null) == true
                ? DiscardMethod.PreserveChildSeriesDiscards
                : DiscardMethod.RaceCountPattern;
        }

        // Discards expected over these races, before looking at any competitor's scores.
        // Child series discards can't be known in advance, so that method expects none.
        private int GetExpectedNumberOfDiscards(IEnumerable<Race> sailedRaces)
        {
            if (DiscardMethod == DiscardMethod.PreserveChildSeriesDiscards)
            {
                return 0;
            }
            return DiscardRaceGroups.Get(DiscardMethod, sailedRaces)
                .Sum(g => GetNumberOfDiscards(g.Count));
        }

        /// <summary>
        /// Apply score code group limitations. After this method runs, scores that exceed the group
        /// limitation will have their Code changed to the overage code and will need re-scoring.
        /// </summary>
        private bool ApplyScoreCodeGroupLimits(
            SeriesResults results,
            SeriesCompetitorResults compResults)
        {
            var allScoreCodeGroups = new List<ScoreCodeGroup>(ScoringSystem?.ScoreCodeGroups ?? []);
            allScoreCodeGroups.AddRange(ScoringSystem?.InheritedScoreCodeGroups ?? []);
            if (allScoreCodeGroups.Count == 0)
            {
                return false;
            }

            var returnValue = false;
            int expectedNumberOfDiscards = GetExpectedNumberOfDiscards(results.SailedRaces);

            foreach (var group in allScoreCodeGroups)
            {
                var thisCompChanges = ApplyGroupLimitToCompetitor(
                    compResults,
                    group,
                    results.SailedRaces.Count(),
                    results,
                    expectedNumberOfDiscards);
                if (thisCompChanges)
                {
                    AddAppliedScoreCodeGroupSummary(results, group);
                }
                returnValue = returnValue || thisCompChanges;
            }

            return returnValue;
        }

        /// <summary>
        /// Apply a single score code group limitation to a competitor's scores.
        /// </summary>
        private bool ApplyGroupLimitToCompetitor(
            SeriesCompetitorResults compResults,
            ScoreCodeGroup group,
            int totalRaces,
            SeriesResults results,
            int expectedNumberOfDiscards)
        {
            // Get all scores for this competitor that are NOT already discarded
            var groupScores = compResults.CalculatedScores.Values
                .Where(cs => !cs.Discard && group.IncludedCodeNames.Contains(cs.RawScore.Code, StringComparer.OrdinalIgnoreCase))
                .ToList();

            if (groupScores.Count == 0)
            {
                return false;
            }

            // Calculate how many scores are allowed
            var allowedCountInfo = CalculateAllowedCountForGroup(
                group, compResults, totalRaces, results, expectedNumberOfDiscards);

            // For date-based limitations, count distinct dates; otherwise count scores
            int effectiveScoreCount;
            if (allowedCountInfo.IsDateBased)
            {
                effectiveScoreCount = groupScores
                    .Select(cs => cs.RawScore.Race?.Date)
                    .Where(d => d.HasValue)
                    .Distinct()
                    .Count();
            }
            else
            {
                effectiveScoreCount = groupScores.Count;
            }

            if (effectiveScoreCount <= allowedCountInfo.AllowedCount)
            {
                return false; // No overage
            }

            // Identify which scores to mark as overage
            var overageScores = SelectOverageScores(groupScores, group, allowedCountInfo);

            // Mark overage scores with the overage code
            foreach (var overageScore in overageScores)
            {
                // Scored as the overage code; results still show the code entered.
                Workspace.CodesBeforeOverage.TryAdd(overageScore.RawScore, overageScore.RawScore.Code);
                overageScore.RawScore.Code = group.OverageCodeName ?? DEFAULT_CODE;
            }
            return true;
        }

        /// <summary>
        /// Calculate how many scores from a group are allowed for a competitor.
        /// Returns an AllowedCountInfo object that distinguishes between race-based and
        /// date-based limitations.
        /// </summary>
        private AllowedCountInfo CalculateAllowedCountForGroup(
            ScoreCodeGroup group,
            SeriesCompetitorResults compResults,
            int totalRaces,
            SeriesResults results,
            int expectedNumberOfDiscards)
        {
            switch (group.LimitationType)
            {
                case ScoreCodeGroupLimitationType.NumberOfRaces:
                    return AllowedCountInfo.RaceBased((int)group.LimitationValue);

                case ScoreCodeGroupLimitationType.PercentOfRaces:
                    {
                        int denominator;
                        if (group.UseNonDiscardedRaces)
                        {
                            // Use total races minus expected discards
                            denominator = totalRaces - expectedNumberOfDiscards;
                        }
                        else
                        {
                            // Use total races
                            denominator = totalRaces;
                        }

                        int allowedCount = (int)Math.Floor(
                            group.LimitationValue / 100m * denominator);
                        return AllowedCountInfo.RaceBased(allowedCount);
                    }

                case ScoreCodeGroupLimitationType.NumberOfDates:
                    {
                        // For date-based limitations, the allowed count is the number of
                        // distinct dates allowed.
                        return AllowedCountInfo.DateBased(
                            (int)group.LimitationValue,
                            group.OverageSelectionMethod);
                    }

                default:
                    return AllowedCountInfo.RaceBased(int.MaxValue); // No limit
            }
        }

        /// <summary>
        /// Select which scores from the group should be marked as overage based on the
        /// overage selection method and limitation type.
        /// </summary>
        private List<CalculatedScore> SelectOverageScores(
            List<CalculatedScore> groupScores,
            ScoreCodeGroup group,
            AllowedCountInfo allowedCountInfo)
        {
            if (allowedCountInfo.IsDateBased)
            {
                return SelectOverageScoresByDate(
                    groupScores,
                    allowedCountInfo.AllowedCount,
                    allowedCountInfo.OverageSelectionMethod);
            }
            else
            {
                return SelectOverageScoresByCount(groupScores, group, groupScores.Count - allowedCountInfo.AllowedCount);
            }
        }

        /// <summary>
        /// Select overage scores when limitation is based on number of races/scores.
        /// </summary>
        private List<CalculatedScore> SelectOverageScoresByCount(
            List<CalculatedScore> groupScores,
            ScoreCodeGroup group,
            int overageCount)
        {
            switch (group.OverageSelectionMethod)
            {
                case ScoreCodeGroupOverageSelection.LatestFirst:
                    // Latest first means we keep the earliest and mark latest as overage
                    // Sort by date DESC, then by race order DESC
                    var sortedByLatest = groupScores
                        .OrderByDescending(cs => cs.RawScore.Race?.Date)
                        .ThenByDescending(cs => cs.RawScore.Race?.Order)
                        .Take(overageCount)
                        .ToList();
                    return sortedByLatest;

                case ScoreCodeGroupOverageSelection.WorstFirst:
                default:
                    // Worst first means highest score values (worst in low-point scoring)
                    var sortedByWorst = groupScores
                        .OrderByDescending(cs => cs.ScoreValue ?? 0m)
                        .Take(overageCount)
                        .ToList();
                    return sortedByWorst;
            }
        }

        /// <summary>
        /// Select overage scores when limitation is based on number of distinct dates.
        /// First identifies which dates are overage based on the overage selection method,
        /// then returns all scores from those overage dates.
        /// </summary>
        private List<CalculatedScore> SelectOverageScoresByDate(
            List<CalculatedScore> groupScores,
            int allowedDateCount,
            ScoreCodeGroupOverageSelection overageSelectionMethod)
        {
            // Group scores by date
            var scoresByDate = groupScores
                .GroupBy(cs => cs.RawScore.Race?.Date ?? DateTime.MinValue)
                .ToList();

            // Select which dates are overage based on selection method
            IEnumerable<IGrouping<DateTime, CalculatedScore>> overageDateGroups;

            switch (overageSelectionMethod)
            {
                case ScoreCodeGroupOverageSelection.LatestFirst:
                    // Latest first means we keep the earliest dates and discard the latest dates.
                    // Sort by date DESC, take the ones beyond allowedDateCount (the most recent dates).
                    overageDateGroups = scoresByDate
                        .OrderByDescending(g => g.Key)
                        .Take(scoresByDate.Count - allowedDateCount);
                    break;

                case ScoreCodeGroupOverageSelection.WorstFirst:
                default:
                    // Worst first means we prioritize marking fewer races as overage by
                    // selecting dates with the fewest races. If multiple dates have the same
                    // number of races, we select the most recent dates (which are often less
                    // critical or more flexible to mark as overage).
                    overageDateGroups = scoresByDate
                        .OrderBy(g => g.Count())      // Fewest races first
                        .ThenByDescending(g => g.Key) // Then most recent date
                        .Take(scoresByDate.Count - allowedDateCount);
                    break;
            }

            // Return all scores from overage dates
            return overageDateGroups
                .SelectMany(g => g)
                .ToList();
        }

        private void CalculateTotals(
            SeriesResults results,
            IEnumerable<Score> scores)
        {
            // 9. Totals
            Recipe.Total.Apply(Workspace, results, scores);
            // 10. Eligibility
            foreach (var eligibility in Recipe.Eligibility)
            {
                eligibility.Apply(Workspace, results);
            }
        }

        private void CalculateRanks(SeriesResults resultsWorkInProgress)
        {
            var orderedComps = resultsWorkInProgress.Results.Values
                .OrderBy(s => s, _competitorComparer);

            int i = 1;
            SeriesCompetitorResults prevComp = null;
            foreach (var comp in orderedComps)
            {
                if (comp.TotalScore == null)
                {
                    comp.Rank = null;
                }
                else if (prevComp != null && _competitorComparer.Compare(comp, prevComp) == 0)
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

        private static int GetNumberOfCompetitors(SeriesResults seriesResults)
        {
            return seriesResults.Competitors.Count;
        }

        // The DNF score in a race, from the DNF code's formula. Other formulas (an average)
        // are taken as boats that started + its Formula Value.
        private decimal? GetDnfScore(Race race, SeriesResults seriesResults = null)
        {
            var dnfCode = FormulaCode(GetScoreCode(DNF_SCORENAME));
            var formula = Formulas.Find(dnfCode);
            // A race or series formula (SER+: boats in the series) gives the DNF score directly
            // (RRS 44.3(c)). An average formula would depend on the scores being worked out, so
            // it falls back to the boats that started + 1.
            if (formula?.Step == FormulaStep.Race
                || (formula?.Step == FormulaStep.Series && seriesResults != null))
            {
                return EvaluateFormula(formula, CodeOnlyScore(dnfCode), dnfCode, race, seriesResults);
            }

            return GetRaceScoresForCounts(race, seriesResults).Count(s => CountsAsStarted(s))
                + (dnfCode?.FormulaValue ?? 0m);
        }

        private decimal? GetDefaultScore(Race race, SeriesResults resultsWorkInProgress)
        {
            if (Recipe.FixedDefaultScore is decimal fixedScore)
            {
                return fixedScore;
            }
            var defaultCode = FormulaCode(GetScoreCode(DEFAULT_CODE));
            var formula = Formulas.Find(defaultCode);
            if (formula?.Step == FormulaStep.Race)
            {
                return EvaluateFormula(formula, CodeOnlyScore(defaultCode), defaultCode, race, resultsWorkInProgress);
            }
            if (IsSeriesBasedScore(defaultCode))
            {
                //Assume it's competitors plus for now. I suppose the default could be
                // the competitors average, but that would create all sort of problems, I think.
                return GetNumberOfCompetitors(resultsWorkInProgress) + (defaultCode?.FormulaValue ?? 0m);
            }

            return GetRaceScoresForCounts(race, resultsWorkInProgress).Count(s => CountsAsStarted(s))
                + (defaultCode?.FormulaValue ?? 0m);
        }

        private bool IsAverage(string code)
        {
            return ((IFormulaHost)this).FormulaFor(code)?.Step == FormulaStep.Average;
        }

        // A score code with a FIX or MAN formula scores its fixed or entered value. Null for
        // any other formula.
        private decimal? GetFixedOrManualValue(CalculatedScore score, ScoreCode scoreCode)
        {
            scoreCode = FormulaCode(scoreCode);
            var formula = Formulas.Find(scoreCode);
            return formula is FixedFormula or ManualFormula
                ? EvaluateFormula(formula, score, scoreCode, score.RawScore?.Race, null)
                : null;
        }

        // A place without a code, or a score taken as-is from a child series.
        private static bool IsUncodedResult(Score s)
        {
            return String.IsNullOrWhiteSpace(s.Code)
                && ((s.Place ?? 0) != 0 || s.PresetScoreValue.HasValue);
        }

        private bool CountsAsFinished(Score s)
        {
            if (s == null)
            {
                return false;
            }
            if (IsUncodedResult(s))
            {
                return true;
            }
            var scoreCode = GetScoreCode(s);
            return CountsAsFinished(scoreCode);
        }

        private bool CountsAsFinished(ScoreCode s)
        {
            return s.Finished ?? false;
        }

        private bool CountsAsStarted(Score s)
        {
            if (s == null)
            {
                return false;
            }
            if (IsUncodedResult(s))
            {
                return true;
            }
            var scoreCode = GetScoreCode(s);
            return CountsAsStarted(scoreCode);
        }

        private bool CountsAsStarted(ScoreCode s)
        {
            return s.Started ?? false;
        }

        private bool CountsAsParticipation(Score s)
        {
            if (s == null)
            {
                return false;
            }
            if (IsUncodedResult(s))
            {
                return true;
            }
            var scoreCode = GetScoreCode(s);
            return CountsAsParticipation(scoreCode);
        }

        private bool CountsAsParticipation(ScoreCode s)
        {
            if (s == null) { return false; }
            return (s.CameToStart ?? false) || (s.CountAsParticipation ?? false);
        }

        private bool CameToStart(Score s)
        {
            if (IsUncodedResult(s))
            {
                return true;
            }
            var scoreCode = GetScoreCode(s);
            return CameToStart(scoreCode);
        }

        private bool CameToStart(ScoreCode s)
        {
            return s.CameToStart ?? false;
        }

        // Averaging calculators: should this score count toward the average?
        // A score code's IncludeInAverage overrides the calculator's own rule.
        private bool IsIncludedInAverage(Score s)
        {
            var scoreCode = GetScoreCode(s);
            return scoreCode?.IncludeInAverage ?? IsIncludedInAverageByDefault(scoreCode);
        }

        // The calculator's rule when the score code doesn't set IncludeInAverage.
        // scoreCode is null for a score without a code.
        private bool IsIncludedInAverageByDefault(ScoreCode scoreCode)
        {
            return Recipe.AveragedScores.IncludesByDefault(scoreCode, Formulas.Find(FormulaCode(scoreCode)));
        }


        private int GetNumberOfDiscards(SeriesResults resultsWorkInProgress)
        {
            return GetExpectedNumberOfDiscards(resultsWorkInProgress.SailedRaces);
        }

        private int GetNumberOfDiscards(int numberOfRaces)
        {
            if (numberOfRaces == 0)
            {
                return 0;
            }
            string[] discardStrings;
            if (String.IsNullOrWhiteSpace(ScoringSystem?.EffectiveDiscardPattern))
            {
                discardStrings = new string[] { "0" };
            }
            else
            {
                discardStrings = ScoringSystem.EffectiveDiscardPattern.Split(',');
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
        // Only scores belonging to series competitors are validated; non-fleet reference
        // scores (included when UseFullRaceScores=true for correct position counting) are
        // intentionally skipped.
        private void ValidateSeries(SeriesResults results, IEnumerable<Score> scores)
        {
            // now that series might filter by fleet, null competitors are possible, so filter them out before validation.
            var seriesCompetitorIds = results.Competitors.Select(c => c.Id).ToHashSet();
            var seriesCompetitorRefs = new HashSet<Competitor>(results.Competitors);

            // Filter to only the scores that belong to series competitors.
            var seriesScores = scores
                .Where(s => seriesCompetitorIds.Contains(s.CompetitorId)
                    || seriesCompetitorRefs.Contains(s.Competitor))
                .ToList();

            var raceIds = results.Races.Select(r => r.Id).ToHashSet();
            var raceRefs = new HashSet<Race>(results.Races, ReferenceEqualityComparer.Instance);

            bool allRacesFound = seriesScores.All(s =>
                raceIds.Contains(s.RaceId)
                || raceRefs.Contains(s.Race)
                || (s.Race != null && raceIds.Contains(s.Race.Id)));

            bool allCompetitorsFound = seriesScores.All(s =>
                seriesCompetitorIds.Contains(s.CompetitorId)
                || seriesCompetitorRefs.Contains(s.Competitor));

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


        private decimal? GetBasicScore(IEnumerable<Score> allScores, Score currentScore)
        {
            return Recipe.RacePoints.Score(Workspace, allScores, currentScore);
        }

        private decimal? GetPerfectScore(IEnumerable<Score> allScores, Score currentScore)
        {
            return Recipe.RacePoints.PerfectScore(Workspace, allScores, currentScore);
        }

        private bool ShouldAdjustOtherScores(Score score)
        {
            return !String.IsNullOrWhiteSpace(score.Code)
            && (GetScoreCode(score)?.AdjustOtherScores ?? true);
        }


        private bool ShouldPreserveScore(Score score)
        {
            return String.IsNullOrWhiteSpace(score.Code)
                || (GetScoreCode(score)?.PreserveResult ?? true);
        }

        private ScoreCode GetScoreCode(Score score)
        {
            if (score == null)
            {
                return null;    
            }
            return GetScoreCode(score.Code);
        }

        // Score code lookups by name for the current run; cleared by StartRun.
        private readonly Dictionary<string, ScoreCode> _scoreCodeCache =
            new(StringComparer.InvariantCultureIgnoreCase);

        private ScoreCode GetScoreCode(string scoreCodeName)
        {
            if (String.IsNullOrWhiteSpace(scoreCodeName))
            {
                return null;
            }
            if (!_scoreCodeCache.TryGetValue(scoreCodeName, out var cached))
            {
                cached = FindScoreCode(scoreCodeName);
                _scoreCodeCache[scoreCodeName] = cached;
            }
            return cached;
        }

        private ScoreCode FindScoreCode(string scoreCodeName)
        {
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
                // Only fall back to DEFAULT_CODE if we are not already looking it up,
                // to prevent infinite recursion when DEFAULT_CODE itself is missing.
                if (!scoreCodeName.Equals(DEFAULT_CODE, CASE_INSENSITIVE))
                {
                    returnScoreCode = GetScoreCode(DEFAULT_CODE);
                }

                if (returnScoreCode == null)
                {
                    returnScoreCode = new ScoreCode
                    {
                        Id = Guid.NewGuid(),
                        Name = "Default",
                        Formula = "FIN+",
                        FormulaValue = 2m,
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
            // The main ordering function is in Core.SeriesService FlattenResults.
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

        private void AddAppliedScoreCodeGroupSummary(SeriesResults results, ScoreCodeGroup group)
        {
            results.AppliedScoreCodeGroupSummaries ??= new List<AppliedScoreCodeGroupSummary>();
            var summary = AppliedScoreCodeGroupSummary.FromGroup(group);

            if (results.AppliedScoreCodeGroupSummaries.Any(s => s.Description == summary.Description))
            {
                return;
            }

            results.AppliedScoreCodeGroupSummaries.Add(summary);
        }

        // 12. The code glossary
        private ScoreCodeSummary GetScoreCodeSummary(string code)
        {
            var codeDef = GetScoreCode(code);
            return new ScoreCodeSummary
            {
                Name = codeDef.Name,
                Description = codeDef.Description,
                Formula = Recipe.Glossary.Formula(Workspace, codeDef)
            };
        }

        private SeriesResults GetResults(Series series)
        {
            StartRun(series);

            SeriesResults returnResults = GatherInitialResults(series);

            SetScores(returnResults, GetScoresForCalculation(series).ToList());
            SetScoresShown(returnResults);

            return returnResults;
        }

        // 12. Each score's place and code as results show them: the place after codes that
        // don't keep it are cleared (and handicap correction), and the code entered, not a
        // score code group's overage code.
        private void SetScoresShown(SeriesResults results)
        {
            foreach (var score in results.Results.Values.SelectMany(r => r.CalculatedScores.Values))
            {
                score.Place = score.RawScore?.Place;
                score.Code = score.RawScore != null
                    && Workspace.CodesBeforeOverage.TryGetValue(score.RawScore, out var entered)
                    ? entered
                    : score.RawScore?.Code;
            }
        }

        // Returns all race scores for non-fleet-filtered series; when a fleet is selected, returns only
        // the scores belonging to fleet competitors.  The distinction between UseFullRaceScores
        // true/false is handled inside GetBasicScore via the UseOriginalPlace flag:
        //   - UseFullRaceScores = false  → GetBasicScore counts fleet competitors ahead → fleet rank
        //   - UseFullRaceScores = true   → GetBasicScore uses score.Place directly → original position
        // These are the run's copies (StartRun): the series' own scores are never modified.
        private IEnumerable<Score> GetScoresForCalculation(Series series)
        {
            var allScores = series.Races.SelectMany(r => Workspace.ScoresIn(r));

            if (series.FleetId == null)
                return allScores;

            var fleetCompetitorIds = series.Competitors.Select(c => c.Id).ToHashSet();
            return allScores.Where(s => fleetCompetitorIds.Contains(s.CompetitorId));
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
            var lastRace = races
                .Where(r => (r.State ?? RaceState.Raced) == RaceState.Raced
                    || r.State == RaceState.Preliminary)
                .OrderBy(r => r.Date).ThenBy(r => r.Order).LastOrDefault();
            if(lastRace == default)
            {
                lastRace = races
                    .Where(r => r.State == RaceState.Abandoned)
                    .OrderBy(r => r.Date).ThenBy(r => r.Order).LastOrDefault();
            }
            if(lastRace == default)
            {
                return new List<Race>();
            }

            return races.Where(r => r.Id != lastRace.Id).ToList();
        }

        private SeriesResults GatherInitialResults(Series series)
        {
            // When PopulateCompetitorsAsync has already built the competitor list (e.g. with
            // fleet filtering), use it directly. Otherwise derive from race scores as before.
            var competitors = series.Competitors?.Any() == true
                ? series.Competitors.ToList()
                : series.Races
                    .SelectMany(r => Workspace.ScoresIn(r).Select(s => s.Competitor))
                    .Where(c => c != null && c.Id != default)
                    .Distinct()
                    .ToList();

            var returnResults = new SeriesResults
            {
                Races = series.Races
                    .OrderBy(r => r.Date)
                    .ThenBy(r => r.Order).ToList(),
                Competitors = competitors,
                Results = new Dictionary<Competitor, SeriesCompetitorResults>()
            };

            returnResults.NumberOfDiscards = GetNumberOfDiscards(returnResults);
            returnResults.DiscardMethod = DiscardMethod;
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

        private void CalculateRaceDependentScores(
            SeriesResults resultsWorkInProgress,
            SeriesCompetitorResults compResults)
        {
            if (Recipe.CodedResults != null)
            {
                Recipe.CodedResults.Apply(Workspace, resultsWorkInProgress, compResults);
                return;
            }
            foreach (var race in resultsWorkInProgress.SailedRaces)
            {
                var score = compResults.CalculatedScores[race];
                var scoreCode = FormulaCode(GetScoreCode(score.RawScore));
                var formula = Formulas.Find(scoreCode);
                if (formula?.Step == FormulaStep.Race)
                {
                    score.ScoreValue = EvaluateFormula(formula, score, scoreCode, race, resultsWorkInProgress);
                }
            }
        }

        private void CalculateSeriesDependentScores(
            SeriesResults resultsWorkInProgress,
            SeriesCompetitorResults compResults)
        {
            // Series counts (SER+) first, so averages use them.
            foreach (var race in resultsWorkInProgress.SailedRaces)
            {
                var score = compResults.CalculatedScores[race];
                var scoreCode = FormulaCode(GetScoreCode(score.RawScore));
                if (IsSeriesBasedScore(scoreCode) && !IsAverage(score.RawScore.Code))
                {
                    score.ScoreValue = EvaluateFormula(Formulas.Find(scoreCode), score, scoreCode, race,
                        resultsWorkInProgress, compResults);
                }
            }

            foreach (var race in resultsWorkInProgress.SailedRaces)
            {
                var score = compResults.CalculatedScores[race];
                var scoreCode = FormulaCode(GetScoreCode(score.RawScore));
                if (IsSeriesBasedScore(scoreCode) && IsAverage(score.RawScore.Code))
                {
                    score.ScoreValue = EvaluateFormula(Formulas.Find(scoreCode), score, scoreCode, race,
                        resultsWorkInProgress, compResults);
                }
            }
        }

        // Series counts and averages: worked out from the series, after race formulas.
        private bool IsSeriesBasedScore(ScoreCode scoreCode)
        {
            return Recipe.SeriesFormulas
                && Formulas.Find(FormulaCode(scoreCode))?.Step is FormulaStep.Series or FormulaStep.Average;
        }

        // The code whose formula and formula value score this code. A COD code scores like the
        // code named in its ScoreLike, followed through further COD codes; it keeps its own
        // flags (came to start, discardable...). Null for a COD with no ScoreLike, or a loop.
        private ScoreCode FormulaCode(ScoreCode scoreCode)
        {
            var seen = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
            while (scoreCode != null && Formulas.Find(scoreCode) is ScoreLikeFormula)
            {
                if (!seen.Add(scoreCode.Name))
                {
                    return null;
                }
                scoreCode = GetScoreCode(scoreCode.ScoreLike);
            }
            return scoreCode;
        }

        private decimal? EvaluateFormula(
            IScoreCodeFormula formula,
            CalculatedScore score,
            ScoreCode scoreCode,
            Race race,
            SeriesResults seriesResults,
            SeriesCompetitorResults compResults = null)
        {
            return formula.Evaluate(new FormulaContext
            {
                Host = this,
                Score = score,
                Code = scoreCode,
                Race = race,
                Series = seriesResults,
                Competitor = compResults
            });
        }

        // A score with just a code, to work out what the code scores (the DNF score for a
        // penalty, the default score for a cap).
        private static CalculatedScore CodeOnlyScore(ScoreCode scoreCode)
        {
            return new CalculatedScore { RawScore = new Score { Code = scoreCode.Name } };
        }

        private IEnumerable<Score> GetRaceScoresForCounts(Race race, SeriesResults seriesResults)
        {
            return Workspace.RaceScoresForCounts(race, seriesResults);
        }

        IScoreCodeFormula IFormulaHost.FormulaFor(string codeName)
        {
            return String.IsNullOrWhiteSpace(codeName) ? null : Formulas.Find(FormulaCode(GetScoreCode(codeName)));
        }

        bool IFormulaHost.CountsAsFinished(Score score) => CountsAsFinished(score);

        bool IFormulaHost.CameToStart(Score score) => CameToStart(score);

        IEnumerable<Score> IFormulaHost.RaceScores(Race race, SeriesResults series) =>
            GetRaceScoresForCounts(race, series);

        decimal? IFormulaHost.DnfScore(Race race, SeriesResults series) => GetDnfScore(race, series);

        bool IFormulaHost.AveragesCountZeroScores => Recipe.ZeroIsAScore;

        ScoreCode IScoringHost.GetScoreCode(Score score) => GetScoreCode(score);

        ScoreCode IScoringHost.GetScoreCode(string codeName) => GetScoreCode(codeName);

        bool IFormulaHost.CountsAsStarted(Score score) => CountsAsStarted(score);

        bool IScoringHost.AdjustsOtherScores(Score score) => ShouldAdjustOtherScores(score);

        bool IScoringHost.CountsAsParticipation(Score score) => CountsAsParticipation(score);

        bool IScoringHost.IsIncludedInAverage(Score score) => IsIncludedInAverage(score);

        bool IScoringHost.IsAverage(string codeName) => IsAverage(codeName);

        decimal? IScoringHost.DefaultScore(Race race, SeriesResults results) => GetDefaultScore(race, results);

        decimal? IScoringHost.FixedOrManualValue(CalculatedScore score, ScoreCode code) =>
            GetFixedOrManualValue(score, code);

        int IScoringHost.DiscardsForRaceCount(int raceCount) => GetNumberOfDiscards(raceCount);

        int IFormulaHost.ExpectedDiscards(SeriesCompetitorResults compResults)
        {
            return DiscardMethod == DiscardMethod.PreserveChildSeriesDiscards
                ? compResults.CalculatedScores.Count(kv =>
                    kv.Key.CompetitorIdsDiscardedInChild?.Contains(compResults.Competitor?.Id ?? Guid.Empty) == true)
                : GetExpectedNumberOfDiscards(compResults.CalculatedScores.Keys);
        }

        // A code that doesn't keep the result (PreserveResult off) shows without a place.
        private void ClearPlacesNotKept(IEnumerable<Score> scores)
        {
            foreach (var score in scores)
            {
                if (!ShouldPreserveScore(score))
                {
                    score.Place = null;
                }
            }
        }


    }
}
