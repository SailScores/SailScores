# Scoring quirks

Behavior of the current scoring calculators that looks odd, or is wrong, but that saved
results depend on. Saved results are recalculated, so **changes to the calculators must
keep the behavior defined here** until the maintainer approves changing it. Fixing one is a separate,
reviewed change (see `FeaturePlans/Scoring-Calculator-Refactor-Plan.md`, "Follow-up work").

Each entry says where the behavior lives and what pins it in tests.

## Score code formulas

### Averages round to one decimal, away from zero - INTENTIONAL

Per Appendix A, Scores ending with .05 (in 2025) and .5 (in earlier systems) should be rounded upward.

AVE, AVE ND and AVE P round to 0.1; AVE W, AVE W ND and AVE W P round to a whole number. All
use `MidpointRounding.AwayFromZero`. Wawasee 2026 then replaces its AVE scores (DNP, DNS, EXC, BKD)
with the boat's average finish, not rounded, as the club's rules say
(`Adjustments/AverageFinishOverCountedRaces`).

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
| Cox-Sprague, Top X | no penalty: see "Coded results score one behind last" below |
| VARC 2026 | no penalty: the base system scores SCP, DPI and ZFP as DNC (the club's answer, `docs/ScoringSystems/Varc2026.md`) |

The low point penalty is capped at the DNF score.

- Pinned by: `AppendixACalculatorTests.CalculateResults_ZFP_*`, `AppendixAPre2025CalculatorTests`,
  `FormulaScenarioTests.PlacePlusPercent_*`, the club fixtures with ZFP/SCP.

### Coded results score one behind last in Cox-Sprague and Top X - Likely bug

Cox-Sprague, Top X and VARC 2026 use `RacePoints/CodedResultsOneBehindLast` in place of the
race formulas: every coded result that came to the start, except TIE, scores one place
behind the last starter (VARC: behind the boats racing). VARC's base system avoids the quirk
for penalties by scoring them as DNC. So a ZFP or SCP doesn't keep its place or take a
percentage penalty, and RET and DSQ score the same as each other. Their PLC% formula
(`HighPointPlacePlusPercentFormula`) is never reached.

- Pinned by: `HighPointScenarioTests.CoxSprague_PlacePlusPercent_ScoresOneBehindLast`,
  `HighPointScenarioTests.TopX_PlacePlusPercent_ScoresOneBehindLast`.
- Status: We haven't been able to find an authoritative definition of how to handle coded scores for Cox-Sprague. Will continue to search for how clubs handle this. Current behavior is more likely correct for RET/DSQ-style codes; probably not for penalties that keep the finish (ZFP, SCP, DPI).

### MAN without code points uses the place

A `MAN` code with no `CodePoints` entered scores the boat's place, if it has one.

- Pinned by: `FormulaScenarioTests.Man_WithoutCodePoints_ScoresPlace`.

## Score code groups

### Worst first compares scores before coded scores are calculated

Limits are applied before score code formulas run, so "worst first" compares place-based
scores. Codes that clear the place all look equal (no boat counts as ahead, so each scores 1), and
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
- Status: Only arises with a score code group whose overage changes whether a code counts as
  a start. Wawasee's base system scores a BKD over its limit as RET, which still starts, so it
  doesn't arise there (the club's answer, 2026-10-18). A club system with, say, a DNC overage
  for a code that starts would still hit it.

### Appendix A pre-2025 has no participation requirement

The `AppendixA.Pre2025` recipe doesn't apply `ParticipationPercent` (its `Eligibility` is
empty), unlike `AppendixA.Standard`. Participation requirements were added to App A systems in late 2026. Clubs that want them should move to the 2025 systems.

- Pinned by: `SeriesOptionScenarioTests.Participation_BoatSailedOneOfThree_RankedOnlyBeforeThe2025Rules`.

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
