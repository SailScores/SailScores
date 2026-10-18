# Scoring Calculator Refactor Plan

## Goals

1. **Readable calculators.** A person who knows the club's rules can read a scoring system's
   code and recognise those rules, without tracing a 1,500-line base class.
2. **Easy to add odd club systems, including by coding agents.** A new club system should
   mostly reuse existing rules, with any genuinely new rule written as a small class that
   can be tested on its own. Nothing a new system does should be able to change an existing
   system's output.

**Status:** approved 2026-10-16 with the recommended decisions (see the end). Phases 0–6 done.

**Constraint (the golden rule):** no existing system's output changes. Saved results are
recalculated, so every phase must leave every characterization snapshot unchanged. Known
bugs are preserved and named, then fixed separately with the maintainer's approval.

---

## Where we are

`SailScores.Core/Scoring` is about 5,500 lines. `BaseScoringCalculator` is 1,542 of them.

### What makes it hard to read and extend

| Problem | Where | Effect |
|---|---|---|
| **One base class does everything** | `BaseScoringCalculator`: race points, score code formulas, score code groups, DNC fill-in, caps, discards, totals, participation, ranking, glossary text, trend, absent-competitor score | You have to read all of it to know what one calculator does |
| **Hooks with misleading names** | `CalculateOverrides` caps scores at the DNC value in the base, but subclasses use it for any post-processing (Wawasee averages, VARC DNC rule). `CalculateTotals` also applies participation, sets display flags and fills columns | Subclasses depend on call order and on what the base did before them |
| **Deep inheritance** | `PbgCalculator → LowPointAveExclDnc → LowPointAveInclDnc → Base`; `Varc2026 → TopXHighPoint → Base`; `PwaStandard → AppAAltFirstIsPoint7 → AppendixA → Base` | Changing a parent silently changes its children, which breaks the golden rule |
| **Copy-paste variants** | `AppAAltFirstIsZero`, `…Point7` and `…Point75` are about 135 lines each and differ by one constant. `AppendixAPre2025` differs from `AppendixA` only in penalty rounding and the participation check | A bug fix has to be made four times |
| **Formula strings handled in six places** (fixed in Phase 2) | `IsAverage`, `IsNonDiscardAverage`, `IsSeriesBasedScore`, `IsTrivialCalculation`, `IsRaceBasedValue`, `GetFormulaString`, plus two `switch` blocks | Adding or changing a formula means finding every list |
| **Hidden mutable state** | `_useOriginalPlace` and `_seriesDiscardMethod` fields; Wawasee/VARC `_racesNotCounted`; input `Score` objects changed in place (`Place` nulled by `ClearRawScores`, `Code` rewritten by score code groups and then restored, handicap times written) | Order of calls matters in ways the code doesn't show |
| **Club rules spread across overrides** | Wawasee replaces `CalculateOverrides`, `DiscardScores` and `CalculateTotals`, and sets `IsPercentSystem = true` so the columns show | Hard to check against the club's rules document |

### What already works and should be kept

- `HandicapScoringCalculator` is a clean decorator: time correction, then any inner calculator.
- `ScoringCalculatorRegistry` puts each system's wiring in one place (settings fields,
  allowed discard methods, columns, total heading).
- Inherited settings are resolved before scoring (`EffectiveSettings`, `EffectiveDiscardPattern`…).
- The header comments on newer calculators (Wawasee, VARC, PBG) state the club's rules in
  plain English. That habit should become the norm.

### Latent issues found (not fixed by this plan, only recorded)

All are listed, with where they live and what pins them, in `docs/Scoring-Quirks.md`. The
fixes are under "Follow-up work" below.

---

## Target design

Scoring becomes a **fixed, readable pipeline** that runs a **recipe**. A recipe names one
small **rule** for each step. Calculators stop being subclasses; each scoring system is a
recipe.

### The pipeline

`ScoringPipeline` (sealed, about 200 lines) is the one place that says how a series is
scored, in order:

```
1. Prepare        races in order, competitors, fleet filtering, discard method      (shared)
2. Count races    which sailed races count                                           CountedRaces rule
3. Race points    points for each finish, including ties                             RacePoints rule
4. Code limits    score code group overages                                          (shared)
5. Missing        a missing result becomes DNC                                       (shared)
6. Coded scores   each score code's formula (FIX, MAN, FIN+, CTS+, SER+, PLC%, AVE…) Formula set
7. Child scores   preset scores from child series                                    (shared)
8. Adjust         caps and club-specific score changes                               Adjustment rules (ordered)
9. Discards       which scores are discarded                                          Discard rule
10. Totals        series total, average, points, plus additions such as penalties    Total rule + additions
11. Eligibility   who may be ranked (participation)                                   Eligibility rule
12. Rank          ordering and tiebreaks                                              Ranking chain
13. Finish        codes used, columns, trend                                          (shared)
```

Each step reads and writes a `ScoringWorkspace`: the series' working copy, private to one run,
with no fields on the calculator and no changes to input `Score` objects.

### A recipe

Each scoring system is one file that reads like the club's rules. For example, Wawasee 2026:

```csharp
// Wawasee 2026 club series ("Low Point Average Scoring System" in their instructions).
//   - Race points are finishing places (Appendix A).
//   - A race counts only when at least MinimumCompetitors boats started a race that day.
//   - Average-scored codes score the boat's average finish over races that count.
//   - Penalty: PenaltyScoreValue for each race short of PenaltyThresholdPercent sailed.
//   - Ranking needs ParticipationPercent of counted races started.
//   - Low total wins; ties go to the lower average finish, then RRS A8.
public static class Wawasee2026
{
    public static ScoringRecipe Create(ScoringSystem system) => new()
    {
        CountedRaces = CountedRaces.MinimumStartersPerDay(system.EffectiveSettings.MinimumCompetitors),
        RacePoints   = RacePoints.LowPoint(),
        Formulas     = Formulas.Standard.WithAverageOver(Races.Counted),
        Discards     = Discards.UncountedRacesOnly,
        Total        = Totals.Sum().Plus(Penalties.ShortOfThreshold(system.EffectiveSettings)),
        Eligibility  = Eligibility.StartedPercent(system.EffectiveParticipationPercent),
        Ranking      = Ranking.LowestTotal().Then(TieBreaks.LowerAverage).Then(TieBreaks.AppendixA8),
    };
}
```

Appendix A and its variants collapse into one parameterised recipe:

```csharp
RacePoints = RacePoints.LowPoint(firstPlace: 0.7m)          // First = .7
Formulas   = Formulas.Standard.With(PenaltyRounding.WholeNumber)  // pre-2025
```

### Rules

Small, stateless classes in `Scoring/Rules/<Step>/`, each with its own tests:

| Step | Interface | Implementations (from today's code) |
|---|---|---|
| Count races | `ICountedRacesRule` | All sailed; minimum starters per day (Wawasee); minimum came-to-start per race (VARC) |
| Race points | `IRacePointsRule` | Low point (first = 1, 0, .7, .75); Top X high point; Cox-Sprague table; high point percentage |
| Formulas | `IScoreCodeFormula` (one class per formula) | FIX, MAN, FIN+, CTS+, SER+, PLC%, TIE, AVE, AVE ND, AVE P, AVE W, AVE W ND, AVE W P. Each class does both its calculation and its glossary text |
| Adjust | `IScoreAdjustment` | Cap at DNC value; Top X/VARC "didn't come to start scores 0"; Wawasee average over counted races |
| Discards | `IDiscardRule` | Pattern (whole series, per day, per child series); keep child series discards; best N count (Top X, VARC); uncounted races only; none |
| Totals | `ITotalRule` + `ITotalAddition` | Sum; low point average with or without DNC; percentage; best N; penalties for races short (addition) |
| Eligibility | `IEligibilityRule` | Appendix A participation %; started-only % (Wawasee); non-discarded races (PBG) |
| Rank | `ITieBreak` chain | Lowest/highest total; zero total ranks last (named quirk); A8.1; A8.2; lower average; VARC A8.1-only; Cox-Sprague; PWA |

A rule that adds a results column declares it, so the registration's `Columns` can be
checked against what the recipe produces.

### Why this helps agents

- **Adding a system is mostly choosing rules.** An agent reads the club's rules, maps each
  rule to the catalog (`docs/Scoring-RuleCatalog.md`), writes only the missing rules as new
  classes, and assembles a recipe. New code can't change existing systems: existing rules
  aren't edited, and recipes don't inherit from each other.
- **Small units with obvious tests.** A new rule is tested on its own with a few lines of
  scenario data. The recipe is tested with the club's worked example.
- **Guard rails that run in CI:** characterization snapshots, invariants, architecture
  tests (rules are stateless, recipes don't subclass, no database access in `Scoring`),
  and a check that every rule class is listed in the catalog.
- **A compact scenario format for tests.** Writing a series by hand today takes 80 lines of
  object setup. A scenario builder makes the club's worked example a few lines:

  ```csharp
  var results = Scenario.With(Systems.Wawasee2026(minimumCompetitors: 4, penaltyPoints: 3))
      .Race("2026-06-06", "A:1 B:2 C:3 D:DNC")
      .Race("2026-06-06", "A:2 B:1 C:DNF D:3")
      .Score();
  ScenarioAssert.Ranks(results, "B", "A", "C", "D");
  ScenarioAssert.Totals(results, ("B", 3), ("A", 3), ("C", 8.5m), ("D", 11));
  ```

- **A project skill** (`.claude/skills/add-scoring-system`) that walks the checklist: rules
  document → catalog mapping → new rules with tests → recipe → registration → base system
  SQL → snapshot variant.

---

## Phases

Each phase is a separate commit (or a short series), with every characterization snapshot
and the local corpus baseline unchanged, followed by a pause for verification. Phases 1–4 refactor inside the current
classes, so the risky part (switching to recipes) only starts once the pieces exist and are
tested.

### Phase 0 — Complete the safety net (done)

1. **Club-restricted base systems in the snapshots.** PBG, VARC 2026, VARC 2026 Shorthanded
   and Wawasee 2026 are in `Fixtures/shared-systems.json` (their SQL scripts were run on local
   dev and the file re-exported: 17 systems). Each is scored over the three variant fixtures,
   adding 12 snapshots. `ScoringFixture.VariantSettings` gives Wawasee the penalty points a
   club child system sets, so its penalty rule runs.
2. **Coverage measured.** `Microsoft.Testing.Extensions.CodeCoverage` added to the unit test
   project (`coverlet.collector` does nothing under Microsoft Testing Platform):
   `dotnet test --project SailScores.Test.Unit/SailScores.Test.Unit.csproj --coverage --coverage-output-format cobertura --coverage-output <file>`.
   `SailScores.Core/Scoring` is at **91.8% of lines** (3,076 of 3,351). The uncovered branches,
   to be pinned in Phase 1 with the scenario builder (writing them with today's 80-line setup
   would be the slow, error-prone way):
   - Score code groups through the calculator: inherited groups, percent of races (with and
     without `UseNonDiscardedRaces`), number of dates, latest-first and worst-first selection,
     by race and by date. (`ScoreCodeGroupTests` only does arithmetic; it never runs a calculator.)
   - Formulas: `MAN` (with and without code points), `AVE W`, `AVE W ND`, `AVE W P`, `SER+` as a
     series-based value, a race-based DNF code (`CTS+`/`FIN+` as the DNF formula), the glossary
     text for `COD`, AVE W* and `MAN`, lower-case formula names.
   - Discard pattern entries that aren't numbers (falls back to the previous race count).
   - Trend when no race is raced, or only abandoned races.
   - Cox-Sprague and Top X `PLC%` penalties; Cox-Sprague discard selection branches.
   - Handicap decorator branches (no rating, no time); PWA comparer tiebreaks past the second level.
3. **Corpus parity harness** (`Characterization/CorpusParityTests`, opt-in, local only). Point
   `FIXTURE_CORPUS_DIR` at a folder of fixtures. The first run writes a baseline of each series'
   results to `<corpus>/.baseline/`; later runs fail listing every series whose results moved.
   This replaced the planned "old against new" harness: a baseline guards every phase, not just
   5 and 6, and doesn't need both implementations alive at once. Checked on the 226-series
   corpus (AWRY 7, GYC 17, LHYC 202): stable across runs, and a tampered baseline is reported.
4. **Quirks named** in `docs/Scoring-Quirks.md`, each with its location and what pins it. Two
   new ones turned up while writing it: averages leave out scores of 0 (wrong under First = 0),
   and `FIX`/`MAN` formula names are matched without case but evaluated with case.

### Phase 1 — Scenario builder, and tests for the coverage gaps (done)

1. **Scenario builder** in `SailScores.Test.Unit/Core/Scoring/Scenarios/` (plural: a
   `Scenario` namespace would clash with the `Scenario` class). `Scenario.With(calculatorType)`
   starts from the shared base system in `Fixtures/shared-systems.json`, so scenarios use the
   codes clubs really have. Race strings: `A:3`, `A:DNC`, `A:ZFP@3` (code with place),
   `A:MAN=4.5` (code points), `A:=6` (preset score). Options: discards, discard method,
   participation, settings, extra codes, code groups (own or inherited), trend, days, fleet
   (with or without full race scores), child series discards. `ScenarioAssert.Ranks`,
   `Rank`, `Totals` and `Scores` print the whole results table on failure.
2. **Converted** `Wawasee2026CalculatorTests` (273 to 193 lines) and `PbgCalculatorTests`
   (193 to 104 lines), assertions unchanged. The arithmetic-only tests in
   `ScoreCodeGroupTests` were replaced by `ScoreCodeGroupScoringTests`, which run a calculator.
3. **Gap tests:** `ScoreCodeGroupScoringTests`, `FormulaScenarioTests`,
   `SeriesOptionScenarioTests` (discard patterns, trend), `HighPointScenarioTests`
   (Cox-Sprague, Top X), `PwaScenarioTests`, `FleetScenarioTests`, plus handicap formula and
   edge cases in `HandicapScoringCalculatorTests`. 66 new tests (58 net of the 8 arithmetic ones removed); every quirk in
   `docs/Scoring-Quirks.md` now names the test that pins it.
4. **Coverage** of `SailScores.Core/Scoring`: 91.8% to 95.4% of lines. What's left:
   - Unreachable: `GetPenaltyScore` in Cox-Sprague and Top X (coded results never reach
     it), PWA tiebreak (C)'s non-zero result (it compares a boat with itself), the PWA (B)
     length checks (every boat has a score in every race).
   - Unused: `SeriesResults.GetResult`, `CalculatedScore.ToString`.
   - Defensive: null-argument guards in each `GetBasicScore`, comparer null branches, "score
     not in series" exceptions, the `CoxSpragueTable` range check, the default case of the
     code group limitation switch.
   - Outside scoring: `ScoringCalculatorFactory` database loading and some
     `ScoringCalculatorRegistry` validation messages (covered by service and controller tests
     elsewhere, or not at all; not part of this refactor).
5. **New quirks found** (all in `docs/Scoring-Quirks.md`, statuses for you to set):
   inherited score code groups ignored unless the system has its own; Cox-Sprague and Top X
   score every coded result that came to the start as one behind last (no PLC% penalty);
   PLC% with a series-based DNF uses starters + 1; PWA tiebreak (C) never decides; a code
   that isn't evaluated (COD) scores 1, like a win.

### Phase 2 — Score code formulas as classes (done)

1. `Scoring/Formulas/`: `IScoreCodeFormula` (`Name`, `Step`, `Evaluate`, `Describe`), one
   sealed class per formula (`FixedFormula`, `ManualFormula`, `FinishersPlusFormula`,
   `CameToStartPlusFormula`, `SeriesCompetitorsPlusFormula`, `PlacePlusPercentFormula`,
   `HighPointPlacePlusPercentFormula`, `AverageFormula`, `AverageNonDiscardedFormula`,
   `AveragePriorFormula`, `TieFormula`, `ScoreLikeFormula`), and `FormulaSet` (`Standard`,
   `HighPoint`, `With(...)`). `FormulaStep` (Race, Series, Average, None) replaces the string
   lists that decided when a formula is evaluated.
2. Formulas reach the calculator only through `IFormulaHost` (code lookup, started/finished
   tests, fleet-filtered race scores, DNF score, expected discards), which
   `BaseScoringCalculator` implements. Phase 4's workspace replaces it.
3. Removed from `BaseScoringCalculator` (1,542 to 1,323 lines): the 13 formula name
   constants, `IsNonDiscardAverage`, `IsTrivialCalculation`, `GetTrivialScoreValue`,
   `IsRaceBasedValue`, `CalculateRaceBasedValue`, `CalculateSeriesBasedValue`, the three
   average methods, `GetPenaltyScore` (and its four overrides) and the glossary switch.
4. Calculator differences are formula replacements: pre-2025 uses whole-number PLC%
   rounding; high point percentage, Cox-Sprague and Top X use `FormulaSet.HighPoint`. VARC's
   DNC rule uses `GetFixedOrManualValue`. The seven copies of the TIE check call
   `TieFormula.IsTie`.
5. Quirks kept and marked in the code: FIX and MAN scored only in upper case, TIE matched
   with case, averages leaving out zeros, AVE ND's best-scores rule, COD not evaluated, the
   SER+ DNF fallback in `GetDnfScore`.
6. One behaviour difference, on data that doesn't exist: a score code with no formula used to
   throw (scoring failed); now it scores like an unknown formula. Local dev has none.
7. `FormulaSetTests`: each formula's step and glossary text, lookup, replacement, and a check
   that the score code editor offers exactly the standard formulas.

### Phase 3 — Ranking as a tiebreak chain (done)

1. `Scoring/Tiebreaks/` (not `Ranking/`: the namespace would clash with the `Ranking` class):
   `ITieBreak`, `Ranking` (`By(...).Then(...)`, first step that separates two boats wins),
   and one sealed class per step: `LowestTotal(zeroTotalRanksLast)`, `HighestTotal`,
   `LowerAverage`, `MoreRacesSailed`, `AppendixA81LowPoint`, `AppendixA81HighPoint`,
   `AppendixA82.LowPoint` / `.HighPoint`, `CountedScoresBestFirst` and
   `LastRaceSkippingUncounted` (VARC), `PwaBeatenMoreOften`,
   `PwaBetterScoresIncludingDiscards`, `PwaBetterDiscard`.
2. `Rankings.LowPoint`, `.HighPoint` and `.Pwa` replace five comparer classes
   (`LowPointSeriesCompComparer`, `HighPointSeriesCompComparer`, `CoxSpragueCompComparer`
   — an exact copy of the high point one — `PwaSeriesCompComparer`,
   `Varc2026SeriesCompComparer`) and Wawasee's private `LowerAverageFirstComparer`. VARC and
   Wawasee build their ranking in their constructor, so the club rule reads in one place.
3. "Zero total ranks last" is now a visible argument, `zeroTotalRanksLast: true`, in the two
   rankings that have it. Wawasee's own first step uses `false`, as before.
4. Equivalences checked by reading, then by the tests: Wawasee's old comparer re-ran the low
   point total comparison after its own; for equal totals that step always ties, so it was
   dropped. The two "`<` should be `<=`" length checks are kept as written, with a note; they
   can't trigger (every boat has a score in every race).
5. New quirk: A8.1 (low point) leaves out scores of 0, so under First = 0 wins don't count in
   a tiebreak. Pinned by `RankingTests`.

### Phase 4 — Discards, totals and eligibility as rules (done)

1. Rules, one sealed stateless class each, by step:
   - `Scoring/Adjustments/` (`IScoreAdjustment`, replacing `CalculateOverrides`):
     `CapAtDefaultScore`, `RaiseToDefaultScore`, `ZeroWhenNotAveraged`,
     `AveragesScoreNothing`, `NotAtStartScoresFixedOrZero`, `RacesNeedStartersEachDay`,
     `AverageFinishOverCountedRaces`.
   - `Scoring/Discards/` (`IDiscardRule`, replacing `DiscardScores`): `WorstScoresByPattern`,
     `WorstPercentagesByPattern(lowerScoreWins, countAveragedRaces)`, `CoxSpragueDiscards`,
     `NoDiscards`, `RacesNeedBoatsAtStart`, `KeepBestScores`, `UncountedRacesOnly`.
   - `Scoring/Totals/` (`ITotalRule`, replacing `CalculateTotals`): `SumOfScores`,
     `LowPointAverage`, `HighPointPercentage`, `CoxSpragueTotals`, `TopXBestScores`,
     `HighPointSum`, `Wawasee2026Totals`.
   - `Scoring/Eligibility/` (`IEligibilityRule`, replacing `ApplyParticipationRequirement` and
     PBG's override): `ParticipationPercent`, `ParticipationOfRacesNotDiscarded`.
2. Each calculator picks its rules in its constructor (`Adjustments`, `Discards`, `Total`,
   `Eligibility`); the virtual `CalculateOverrides`, `DiscardScores` and `CalculateTotals` are
   gone, as are 29 overrides and helpers across nine calculators. Wawasee 2026, VARC 2026 and
   PBG are now constructor-only. `BaseScoringCalculator` is down to 1,253 lines;
   the top level of `SailScores.Core/Scoring` is 3,917 lines (5,744 with the step folders, most of the growth being header comments).
3. `ScoringWorkspace`, one per run, holds the resolved discard method, the fleet place mode
   (`UseOriginalPlace`) and `RacesNotCounted`; the `_useOriginalPlace`, `_seriesDiscardMethod`
   and both `_racesNotCounted` fields are gone. Rules ask calculator-specific questions through
   `IScoringHost` (extends Phase 2's `IFormulaHost`). The calculator still holds the current
   workspace in one property, because `GetBasicScore` and the VARC A8.2 tiebreak read it;
   Phase 5's pipeline passes it explicitly instead.
4. Kept as found: Wawasee and VARC work out counted races per competitor, inside the
   adjustment and discard steps, because the result depends on score code group rewrites made
   while earlier competitors were scored (new quirk, pinned). LowPointAveExcl's totals were an
   exact copy of LowPointAveIncl's; both use `LowPointAverage`. Top X computed two values it
   never used; dropped.
5. New quirks, pinned by tests: counted races depend on scoring order (and Wawasee totals can
   disagree with the scores shown); Cox-Sprague can discard a non-discardable code; Appendix A
   pre-2025 has no participation requirement.

### Phase 5 — Pipeline and recipes; Appendix A family first (done)

1. `ScoringRecipe` (name, race points, formulas, adjustments, discards, total, eligibility,
   ranking) and `ScoringPipeline`, which scores any recipe. `Scoring/RacePoints/`
   (`IRacePointsRule`, `LowPointPlaces` with `AppendixA`, `FirstIsZero`, `FirstIsPoint7`,
   `FirstIsPoint75`) replaces five copies of the place and tie scoring.
2. Recipes in `Scoring/Systems/`: `AppendixA` (`Standard`, `Pre2025`, `FirstIsZero`,
   `FirstIsPoint7`, `FirstIsPoint75`), `PwaStandard`, and `Wawasee2026` (moved up from Phase 6:
   it subclassed `AppendixACalculator`). Registry entries use `Pipeline(...)`.
3. Deleted: `AppendixACalculator`, `AppendixAPre2025Calculator`, `AppAAltFirstIsZero`,
   `AppAAltFirstIsPoint7`, `AppAAltFirstIsPoint75`, `PwaStandardCalculator`,
   `Wawasee2026Calculator`. Tests build calculators through the registry; factory tests
   check recipe names.
4. To avoid copying 1,250 lines, `ScoringPipeline` is for now a sealed subclass of
   `BaseScoringCalculator` that only supplies the recipe's rules. Phase 6 moves the base class
   code into `ScoringPipeline` once no other subclass is left, and passes the workspace
   explicitly then.
5. New quirk: First = .75 counts first as 0.7 in a tie (copied from First = .7). Pinned.

### Phase 6 — Remaining systems, one per commit (done)

1. New recipe members for the hooks the last calculators overrode: `CodedResults`
   (`RacePoints/CodedResultsOneBehindLast`, Cox-Sprague and Top X), `SeriesFormulas`
   (off for Cox-Sprague), `FixedDefaultScore` (0 for Top X), `AveragedScores`
   (`Totals/AveragedScores`, low point average without DNCs) and `Glossary`
   (`Scoring/Glossary/`: standard, codes not at the start excluded, codes not averaged
   excluded, Cox-Sprague, Top X).
2. Recipes, one commit each: `LowPointAverages` (`IncludingDnc`, `ExcludingDnc`, `Pbg`),
   `HighPointPercentages`, `CoxSprague`, `TopX` (`Create`, `Varc2026`). Race points:
   `LowPointPlaces.LowPointAverage`, `HighPointPercentagePoints`, `CoxSpraguePoints`,
   `TopXPoints` (each merging the whole-race and fleet copies of the place counting).
3. Tiebreaks now take the run's workspace like other rules (VARC's A8.2 reads
   `RacesNotCounted` from it); `Ranking` is no longer an `IComparer`.
4. `BaseScoringCalculator` is gone: its code is in `ScoringPipeline` (sealed, no virtual
   members), with the steps numbered in its header and in `ScoreCompetitor`. Every
   calculator subclass is deleted.
5. Deviation from the plan: the pipeline still holds the current run's workspace in a
   private property instead of passing it to every private method, because
   `IScoringHost`'s answers (fleet place mode, discard method) depend on it and rules call
   the host. Rules get it explicitly. Phase 7 can revisit this if it moves those answers into
   the workspace.
6. New quirk: low point averages ignore "use full race scores" (pinned).
### Phase 7 — Stop mutating inputs (done)

1. Each run copies the series' scores into its workspace (`ScoringWorkspace.RaceScores`;
   `Score.ShallowCopy`). Rules and the pipeline read a race's scores with
   `workspace.ScoresIn(race)`, never `Race.Scores`.
2. Places cleared for codes that don't keep them (`ClearPlacesNotKept`) and score code group
   overage codes change only the copies. `CaptureOriginalScoreCodes` /
   `RestoreOriginalScoreCodes` are gone: the workspace records the code each overage
   replaced (`CodesBeforeOverage`).
3. Results carry what they show: `CalculatedScore.Place` and `CalculatedScore.Code` (step 12).
   `SeriesService.FlattenScores` reads them. `RawScore` is the run's copy, as scored.
4. Handicap time correction is an input step (`IScoreInputStep`) that
   `HandicapScoringCalculator` adds to the inner pipeline (`ScoringPipeline.WithInputStep`).
   It runs on each run's copies, so the trend's rerun and the absent-competitor score
   correct their own copies as before.
5. `InputsUnchangedTests` checks every system leaves the series' scores unchanged (with a
   fleet, a trend and the absent-competitor score). Snapshots and the corpus are unchanged.
6. Not changed: the pipeline still keeps the current run's workspace in a private property
   (see Phase 6, item 5). Every host answer now reads the workspace, so passing it to each
   private method would be mechanical; left for a later tidy-up.

### Phase 8 — Agent guidance and guard rails

- Rewrite `docs/Scoring-AddingACalculator.md` around recipes: rules document → catalog →
  new rules → recipe → registration → SQL → tests → snapshot.
- `docs/Scoring-RuleCatalog.md`: every rule, what it does in one sentence, and which systems
  use it. A test fails if a rule class is missing from it.
- Architecture tests: rules have no instance fields (stateless), recipes are static
  factories, nothing in `Scoring` references `Database`, and no new subclass of a recipe or rule
  outside `Rules/`.
- `.claude/skills/add-scoring-system/SKILL.md` that runs the checklist.
- Update `CLAUDE.md` and `.github/copilot-instructions.md`.

### Afterwards

See "Follow-up work".

---

## Risks

| Risk | Mitigation |
|---|---|
| A subtle output change slips through | Snapshots on 14 fixtures and 51 variants; corpus baseline over every local fixture (226 today); invariants |
| Corpus doesn't exercise a branch | Phase 0 coverage pass; scenario tests for every rule |
| Recipes become a second inheritance tree | Architecture test: recipes are static factories, not subclasses; rules are sealed |
| Rule explosion (one rule per club) | Catalog review in the checklist: prefer a parameter on an existing rule. Club-only rules live in `Rules/Club/` with the club named |
| Long-running branch drifts | Phases 1–4 are useful on their own and merge independently; Phases 5–6 switch one system at a time behind the registry |
| SeriesService preparation (summary roll-ups, fleet competitor lists) is also complex | Out of scope here; the pipeline's Prepare step takes the series as SeriesService builds it today. A follow-up plan can cover it |

## Decisions (approved 2026-10-16)

1. **Composition (recipes and rules)** rather than a slimmer inheritance tree, because
   inheritance is what lets a parent change silently alter its children.
2. **Known bugs stay until the refactor is done**, then each is fixed as an approved change.
3. **SeriesService's preparation is out of scope** for this plan (see follow-up work).
4. **Plain xUnit helper methods** in the scenario builder, no assertion library.

---

## Follow-up work

Not part of Phases 1–8. Each item is its own change. The behaviour changes need the
maintainer's approval, a reviewed snapshot diff, and a note saying which saved results move
(the corpus baseline shows which series).

### Behaviour fixes (change saved results)

| Fix | Where | Notes |
|---|---|---|
| **A total of 0 should rank first under First = 0** | `LowestTotal(zeroTotalRanksLast: true)` in `Rankings.LowPoint` | Give the First = 0 calculator a ranking with `zeroTotalRanksLast: false`, not the shared one. Unskip `AppAAltFirstIsZero_CompetitorWhoWonEveryCountedRace_RanksFirst`. A SQL query (recursive CTE over scoring system roots, CalculatorType 3, competitors whose wins equal races) finds affected series in production |
| **Counted races shouldn't depend on scoring order** | `RacesNeedStartersEachDay`, `RacesNeedBoatsAtStart` | Work out counted races once, after all score code group overages are applied (the pipeline's "count races" step, after "code limits"). Changes results only where an overage changes whether a code is a start |
| **First = .75 tie for first should use 0.75** | `LowPointPlaces.FirstIsPoint75` (`tiedFirstPlace: 0.7m`) | Change to 0.75m. Affects First = .75 series with a tie for first |
| **Cox-Sprague should keep non-discardable codes** | `Discards/CoxSpragueDiscards` | Filter candidates by `Discardable`, as the unused list intended |
| **Appendix A pre-2025 participation** | `AppendixAPre2025Calculator` (no `Eligibility`) | Decide whether pre-2025 systems should apply `ParticipationPercent`. Changes results only for pre-2025 systems with a percent set |
| **Averages should include scores of 0 under First = 0** | `Formulas/Averages.Averaged` | Same recipe-only approach. Check first whether any First = 0 club uses AVE codes |
| **AVE ND should average the non-discarded scores** | `AverageNonDiscardedFormula` | Today it averages the best (count − average codes − expected discards) scores, not the discards actually chosen. Confirm the intended rule first |
| **`COD`: implement "scored like another code", or remove it** | Formula set; glossary | No shared system or corpus fixture uses it. Removing means dropping it from the formula choices in the score code editor |
| **Formula names matched without case everywhere** | `FixedFormula`, `ManualFormula`, `TieFormula.IsTie` | Local dev has only upper-case formulas, so no saved result should change; confirm on production |
| **Inherited score code groups should apply without an own group** | `ApplyScoreCodeGroupLimits` (after Phase 4: the code limits step) | Affects club child systems whose groups are on the parent. Check production for systems with inherited groups and none of their own |
| **Cox-Sprague and Top X: PLC% codes should keep the place and take a penalty** | `CalculateRaceDependentScores` overrides | If confirmed: score PLC% codes with `HighPointPlacePlusPercentFormula` (already in their formula set, never reached), leaving RET/DSQ-style codes as one behind last. Also VARC 2026, which inherits it |
| **PLC% with a series-based DNF should use the DNF score** | `BaseScoringCalculator.GetDnfScore` | RRS 44.3(c). Changes ZFP/SCP/DPI scores in races where a series boat didn't start |
| **A8.1 should count scores of 0 under First = 0** | `Tiebreaks/AppendixA81LowPoint` | A parameter, set only in the First = 0 ranking. Fix together with the two other zero quirks. Tell a real 0 (a First = 0 win) from a score that is still 0 or null because it isn't worked out yet at that point (maintainer's note) |
| **PWA tiebreak (C) compares the wrong boat** | `Tiebreaks/PwaBetterDiscard` | One-word fix (`y.CalculatedScores`). Only matters when non-discardable codes make discards differ |
| **Low point averages should honour "use full race scores"** | `LowPointPlaces.LowPointAverage` (`useWholeRacePlace: false`) | Use the whole-race place when the series uses full race scores, as Appendix A does (`useWholeRacePlace: true`). Changes results only for fleet-filtered low point average and PBG series with full race scores. Update `FleetScenarioTests.FleetWithFullRaceScores_LowPointAverageStillRecalculatesPlaces` |
| **Check the club systems against each club's Sailing Instructions** | VARC 2026, VARC 2026 Shorthanded, Wawasee 2026, PBG; their score codes (`Sql Utility Scripts/Create … Scoring system(s).sql`) | So far they are checked against tests written from the rules as described. Compare each with the club's current SIs and a scored sheet from the club, including every score code. VARC's codes are copied from Top X High Point, where several are fixed 0 points; that is probably wrong for VARC (for example a boat that came to the start and retired). Fixing a code is a data change to those systems, not a calculator change |
| **Wawasee 2026 shouldn't need `IsPercentSystem` to show its columns** | `Wawasee2026Calculator`, results views | Display only: let a recipe declare which per-race values show |

### Other code to refactor

| Area | Why | Suggested approach |
|---|---|---|
| **`SeriesService` scoring preparation**: `CalculateScoresAsync`, `AddChildSeriesAsSeries`, `AddChildSeriesAsRaces`, `AddAbsentCompetitorScoresAsync`, `PopulateCompetitorsAsync`, `ApplyFleetFilterToList`, the `PopulateSummary*` methods | A 1,734-line service mixing loading, summary-series roll-up, fleet filtering, scoring and saving. Summary series are a common source of scoring bugs | Its own plan: a `SeriesScoringInputBuilder` that turns database rows into the pipeline's input, tested with the same fixtures. The corpus baseline guards it |
| **`SeriesService` flattening**: `FlattenResults`, `FlattenScores`, `FlattenRaces`, `FlattenCompetitors`, `SaveHistoricalResults`, chart data | Turning results into `FlatResults` is mixed into the same class | Move to a `FlatResultsBuilder`; snapshots already pin its output |
| **`ScoringCalculatorFactory`** | Lives in `Scoring` but reads the database, which would fail the Phase 8 "no database in `Scoring`" architecture test | Move the loading into `ScoringService`; keep the pure `CreateScoringCalculator` |
| **`ScoringService` inheritance resolution**: `ApplyInheritedValues`, `GetAncestorSystemsAsync`, `GetAllCodesAsync`, `GetAllCodeGroupsAsync` | The effective system (codes, groups, settings, discard pattern) is put together in several places | One `EffectiveScoringSystem` builder, so the pipeline gets a fully resolved system |
| **Trend** (`AddTrend`) | Rescores the series without its last race or day inside the calculator | A separate step after scoring, if Phase 5 doesn't absorb it cleanly |
| **Dead code** | `SeriesResults.GetResult`, `CalculatedScore.ToString` (the unreachable Cox-Sprague and Top X `GetPenaltyScore` overrides went in Phase 2) | Delete in a tidy-up |
| **`coverlet.collector` package** | Unused under Microsoft Testing Platform | Remove from the unit test project (and other test projects, if the same) |
| **Score code editor UI** | The formula choices are a hard-coded list in `ScoreCodeWithOptionsViewModel.FormulaOptions`. `FormulaSetTests` now fails if it and `FormulaSet.Standard` differ | Give formulas an editor description and build the list from `FormulaSet.Standard` |
