# Scoring quirks

Behaviour of the current scoring calculators that looks odd, or is wrong, but that saved
results depend on. Saved results are recalculated, so **any rewrite of the calculators must
keep every quirk here** until the maintainer approves changing it. Fixing one is a separate,
reviewed change (see `FeaturePlans/Scoring-Calculator-Refactor-Plan.md`, "Follow-up work").

Each entry says where the behaviour lives and what pins it in tests.

## Ranking

### A total of 0 ranks last - BUG

`LowestTotal(zeroTotalRanksLast: true)`, the first step of `Rankings.LowPoint` and `Rankings.Pwa`, treats a total of 0 like no total
(`decimal.MaxValue`). Under Appendix A, First = 0, a boat that wins every counted race totals
0 and ranks last.

- Pinned by: `Snapshots/SharedSystems/gyc_2026_helm-of-the-year__Appendix-A-Alternative---First---0.verified.json`;
  skipped test `ScoringInvariantTests.AppAAltFirstIsZero_CompetitorWhoWonEveryCountedRace_RanksFirst`.
- Status: **bug**, fix after the refactor with approval.

### PWA tiebreak (C) never decides - BUG

`Tiebreaks/PwaBetterDiscard` builds both boats' discard lists from
`x`, so the comparison is always equal and ties go on to the last-race tiebreak (E).

- Pinned by: `PwaScenarioTests` (the class comment); no test isolates it, as (C) only matters
  when non-discardable codes make two boats' discards differ.
- Status: **bug**, fix after the refactor with approval.

### A8.1 tiebreak leaves out scores of 0 - BUG

`Tiebreaks/AppendixA81LowPoint` compares only non-discarded scores that aren't 0. Under
Appendix A, First = 0, a boat's wins are left out of the tiebreak, so a tie can go to the
wrong boat.

- Pinned by: `RankingTests.AppendixA81_FirstIsZero_LeavesOutWins`.
- A bug for First = 0, like the two other zero quirks, but need to distinguish between 0 scores and other codes that might leave a 0 for score through parts of the process.

## Score code formulas

### Averages leave out scores of 0 - BUG

`Formulas/Averages.Averaged`, used by every average formula, skips any score
of 0 as well as other average-scored races. Under First = 0, a boat's wins (0 points) are left
out of its average, so an average-scored race scores worse than the boat's real average.

- Pinned by: `FormulaScenarioTests.Average_FirstIsZero_LeavesOutWins`.
- Status: **bug** for First = 0; harmless where no race scores 0.

### Averages round to one decimal, away from zero - INTENTIONAL

Per Appendix A, Scores ending with .05 (in 2025) and .5 (in earlier systems) should be rounded upward.

AVE, AVE ND and AVE P round to 0.1; AVE W, AVE W ND and AVE W P round to a whole number. All
use `MidpointRounding.AwayFromZero`.

- Pinned by: `AppendixACalculatorTests.CalculateResults_AveragePrior_UsesHalf`, `IncludeInAverageTests`.

### AVE ND counts the best scores, not the non-discarded ones - LIKELY BUG

`AverageNonDiscardedFormula` averages the lowest (count − average codes − expected discards)
scores. It doesn't use the discards actually chosen. If no scores are left, it takes the
single best score.

- Pinned by: `FormulaScenarioTests.AverageNonDiscarded_AveragesBestScoresNotUndiscardedOnes`.
- Status: **likely bug**. Confirm against the rules wording, then fix after the refactor with approval.

### PLC% penalty rounding differs by calculator

A PLC% code adds a penalty to the boat's place (low point) or takes it off the points (high
point). The penalty is a percentage of the DNF score, or of the fleet size, rounded:

| Calculator | Rounding of the penalty |
|---|---|
| Every calculator not listed below (`PlacePlusPercentFormula`, `Rounding2025`) | to 0.1, away from zero |
| Appendix A pre-2025 (`PlacePlusPercentFormula`, `RoundingPre2025`) | to a whole number, away from zero |
| High point percentage (`HighPointPlacePlusPercentFormula`) | percentage of fleet size, to a whole number |
| Cox-Sprague, Top X, VARC 2026 | no penalty: see "Coded results score one behind last" below |

The low point penalty is capped at the DNF score.

- Pinned by: `AppendixACalculatorTests.CalculateResults_ZFP_*`, `AppendixAPre2025CalculatorTests`,
  `FormulaScenarioTests.PlacePlusPercent_*`, the club fixtures with ZFP/SCP.

### PLC% with a series-based DNF uses starters + 1 - BUG

`ScoringPipeline.GetDnfScore` evaluates a DNF formula worked out from the race alone (FIX, MAN, FIN+, CTS+), and otherwise returns boats
that started + 1. So when DNF is SER+ (boats in the series + 1, as in the Appendix A regatta
systems), the penalty is a percentage of starters + 1, not of the DNF score. A boat that
missed the race lowers everyone's penalty in it.

- Pinned by: `FormulaScenarioTests.PlacePlusPercent_DnfSeriesCompetitorsPlusOne_PenaltyFromStartersPlusOne`.
- RRS 44.3(c) bases the penalty on the DNF score. GetDnfScore should always get the correct DNF score for a race. Need to avoid calculation loops, though.

### Coded results score one behind last in Cox-Sprague and Top X - Likely BUG

Cox-Sprague, Top X and VARC 2026 use `RacePoints/CodedResultsOneBehindLast` in place of the
race formulas: every coded result that came to the start, except TIE, scores one place
behind the last starter. So a ZFP or SCP doesn't keep its place or take a
percentage penalty, and RET and DSQ score the same as each other. Their PLC% formula
(`HighPointPlacePlusPercentFormula`) is never reached.

- Pinned by: `HighPointScenarioTests.CoxSprague_PlacePlusPercent_ScoresOneBehindLast`,
  `HighPointScenarioTests.TopX_PlacePlusPercent_ScoresOneBehindLast`.
- Status: We haven't been able to find an authoritative definition of how to handle coded scores for Cox-Sprague. Will continue to search for how clubs handle this. Current behavior is more likely correct for RET/DSQ-style codes; probably not for penalties that keep the finish (ZFP, SCP, DPI).

### Formula names: matched without case, evaluated with case - BUG

`FormulaSet.Find` matches `FIX` and `MAN` ignoring case, but `FixedFormula` and
`ManualFormula` only score the exact upper-case name. A code with formula `fix` scores null (no points). Race-based
and series-based formulas use `ToUpperInvariant()` and work in any case.

- Pinned by: `FormulaScenarioTests.Fix_LowerCase_ScoresNothing`. Every shared system uses upper case.
- Status: **bug**, fix after the refactor with approval.

### First = .75 counts first as 0.7 in a tie - BUG

`LowPointPlaces.FirstIsPoint75` scores first as 0.75, but a tie for first averages 0.7 for
first place: the value was copied from the First = .7 calculator.

- Pinned by: `FormulaScenarioTests.TieForFirst_AveragesFirstPlaceValueWithSecond`.
- Status: marked bug as it is an obvious copy error; fix after the refactor with approval.

### MAN without code points uses the place - INTENTIONAL

A `MAN` code with no `CodePoints` entered scores the boat's place, if it has one.

- Pinned by: `FormulaScenarioTests.Man_WithoutCodePoints_ScoresPlace`.

### COD is described but never evaluated - BUG

The glossary (`ScoreLikeFormula.Describe`) describes `COD` as "Scored like {ScoreLike}", but no
scoring step handles it, so the score is whatever the place-based score gave. For a code that
doesn't preserve the result the place is cleared, no boat counts as ahead of it, and it scores
1, like a win. No shared system or corpus fixture uses COD.

- Pinned by: `FormulaScenarioTests.Cod_IsNotEvaluated`.
- Status: **unfinished feature**. Implement or remove after the refactor, with approval.

## Score code groups

### Inherited groups apply only when the system has a group of its own - BUG

`ApplyScoreCodeGroupLimits` returns early when the system's own `ScoreCodeGroups` is empty,
before adding `InheritedScoreCodeGroups`. A club child system with no groups of its own
ignores its parent's groups.

- Pinned by: `ScoreCodeGroupScoringTests.InheritedGroupOnly_IsNotApplied`,
  `ScoreCodeGroupScoringTests.InheritedGroupWithOwnGroup_IsApplied`.

### Worst first compares scores before coded scores are calculated

Limits are applied before score code formulas run, so "worst first" compares place-based
scores. Codes that clear the place all look equal (each scores 1, see the COD entry), and
the earliest uses are kept.

- Pinned by: `ScoreCodeGroupScoringTests.NumberOfRaces_WorstFirst_KeepsLowestPlace`.

### Overage scores count in averages

A use over the limit scores as the overage code (DNC), and an average-scored use within the
limit averages in those DNC points. Results show the original code with the overage points.

- Pinned by: `ScoreCodeGroupScoringTests.NumberOfRaces_LatestFirst_LaterUseScoresDnc`.

### Counted races depend on the order competitors are scored in - BUG

Wawasee 2026 (`RacesNeedStartersEachDay`) and VARC 2026 (`RacesNeedBoatsAtStart`) work out
which races count once for each competitor, as part of scoring that competitor. Score code
group overages rewrite each competitor's codes as it is scored, so a later competitor can see
a different count of starters. Wawasee's totals then use the races left by the last competitor
scored, so a boat's shown scores and its total can disagree.

- Pinned by: `RuleTimingScenarioTests`.
- Status: May need clarification from creators of this system. Only arises with a score code group whose
  overage changes whether a code counts as a start (Wawasee's BKD limit does).

### Cox-Sprague can discard a code that isn't discardable - BUG

`CoxSpragueDiscards` picks from every race the competitor came to the start in. The old code
built a discardable-only list but never used it.

- Pinned by: `HighPointScenarioTests.CoxSprague_NonDiscardableCodeAtStart_CanBeDiscarded`.

### Appendix A pre-2025 has no participation requirement - MINOR BUG

The `AppendixA.Pre2025` recipe doesn't apply `ParticipationPercent` (its `Eligibility` is
empty), unlike `AppendixA.Standard`.

- Pinned by: `SeriesOptionScenarioTests.Participation_BoatSailedOneOfThree_RankedOnlyBeforeThe2025Rules`.
- Minor bug: Participation requirements were added to App A systems in late 2026. Clubs should not still be using the pre-2025 systems if they want new behavior.

### Low point averages ignore "use full race scores" - BUG

`LowPointPlaces.LowPointAverage` always counts the boats ahead among the scores being scored,
so in a fleet series a boat's place is recalculated within the fleet even when the series
uses full race scores. Appendix A keeps the whole-race place.

- Pinned by: `FleetScenarioTests.FleetWithFullRaceScores_LowPointAverageStillRecalculatesPlaces`.
- Status: **bug**, fix after the refactor with approval.

## Scores as shown

Until Phase 7 these changed the series' own `Score` objects. Scoring now changes only the
run's copies (`ScoringWorkspace.RaceScores`), and results carry what they show in
`CalculatedScore.Place` and `CalculatedScore.Code`. What results show didn't change.

- Pinned by: `InputsUnchangedTests` (every system leaves the series' scores as they were).

### A code that doesn't keep the result shows no place

`ClearPlacesNotKept` clears the place of every code that doesn't preserve the result
(`PreserveResult` false), before race points. Results then show the code without a place
(`DNF 12`, not `5:12`), and no other boat counts it as ahead.

- Pinned by: every snapshot with a non-preserving code (for example `DNF`, `DNC`, `OCS` in
  `lhyc_2018_mc-holiday`); `InputsUnchangedTests.CalculateResults_CodeThatDoesNotKeepThePlace_ShowsNoPlace`.

### Score code group overages show the code entered

A score over a group's limit is scored as the overage code (`RawScore.Code`), but results show
the code entered (`CalculatedScore.Code`, from `ScoringWorkspace.CodesBeforeOverage`). The
codes used glossary lists the overage code.

- Pinned by: `ScoreCodeGroupScoringTests.NumberOfRaces_LatestFirst_LaterUseScoresDnc`,
  `AppendixACalculatorTests.CalculateResults_WithScoreCodeGroupLimit_LeavesInputCodesUnchanged`.

### Handicap scoring ranks on corrected time

Handicap time correction is an input step of the pipeline (`IScoreInputStep`): it sets the
corrected time, handicap value and corrected-time place of each copy, clears the place of a
rated boat with no elapsed time, and gives a boat with no handicap the code `NHC`.

- Pinned by: `HandicapScoringCalculatorTests` (`CalculateResults_CompetitorWithNoHandicap_AssignsNhcCode`,
  `CalculateResults_RatedBoatWithoutElapsedTime_HasNoPlace`, `CalculateResults_TimedRace_LeavesSeriesScoresUnchanged`).

## Display

### Wawasee 2026 sets IsPercentSystem

So that the per-race "of a possible" values show, Wawasee 2026 sets `IsPercentSystem = true`,
though it is a low point system. Its snapshots show race scores as `place/1`.

- Pinned by: `Snapshots/SharedSystems/*__Wawasee-2026.verified.json`.
