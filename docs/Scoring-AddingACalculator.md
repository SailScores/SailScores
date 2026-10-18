# Adding a Scoring Calculator

How to add a new way of scoring (a "calculator") and the shared base scoring system that clubs build on. Written for both human developers and coding agents: follow the steps in order and check off the [checklist](#checklist) at the end.

Most new base systems come with a new calculator, so plan on a code change and a deployment. Clubs don't write their own scoring logic. They create child systems in the database from the shared base systems they're allowed to use.

---

## How scoring systems fit together

| Concept | Where | Notes |
|---|---|---|
| **Recipe** | `SailScores.Core/Scoring/Systems/*.cs` | A system's scoring logic: the rule for each step, run by `ScoringPipeline`. One recipe can serve several calculator types. |
| **Calculator type** | `SailScores.ApiClient/Enumerations/ScoringCalculatorType.cs` | Int enum stored on base systems. **Never renumber or reuse a value.** |
| **Registration** | `SailScores.Core/Scoring/ScoringCalculatorRegistry.cs` | Per type: how to create the calculator, settings fields, allowed discard methods, extra results columns, total column heading. The UI and validation read this. |
| **Base system** | `ScoringSystems` row with `ClubId` and `ParentSystemId` null | Sets `CalculatorType` and the defaults. Created by a SQL script. Clubs must never edit or delete it. |
| **Club system** | `ScoringSystems` row with `ClubId` set | A child of a base system or of another club system. Edited on the club's Scoring System Edit page. |
| **Club access** | `ScoringSystemClubAccess` and `ScoringSystem.IsRestrictedToClubs` | Site Admin > Shared Scoring Systems. A restricted system is only offered as a parent to the listed clubs. |

### Inheritance (resolved by `ScoringService.ApplyInheritedValues`)

A child system inherits from its parent chain:

- `CalculatorType` always comes from the root base system. Children never store one.
- `Settings` (JSON, `Core/Model/ScoringSystemSettings.cs`) merge property by property; the nearest non-null value wins. Calculators read `ScoringSystem.EffectiveSettings`.
- `DiscardMethod`: the nearest non-null value, else `RaceCountPattern`. Rules read `ScoringWorkspace.DiscardMethod`, which is resolved for each series.
- `DiscardPattern` and `ParticipationPercent`: the nearest non-blank value. Calculators read `EffectiveDiscardPattern` and `EffectiveParticipationPercent`.
- Score codes and score code groups: the child's own codes plus inherited ones.

**Calculators must read the effective values, never a system's own values.** A club's child system usually leaves most values blank.

---

## The golden rule: don't change how an existing calculator scores

Saved series are recalculated (when a race changes, when an admin recalculates), so a change to an existing calculator quietly rewrites historical results for every club using it. For a club-specific need:

- **Add a new calculator type** (e.g. VARC 2026 was added alongside Top X High Point instead of changing it), or
- **Add a setting** whose null/default value keeps today's behavior exactly, and set it only on the new base system.

Fixing a genuine bug in an existing calculator is a separate decision that needs the maintainer's approval, plus a note in the commit about which results change.

Known odd behaviour that saved results depend on is listed in `docs/Scoring-Quirks.md`. Keep it unless a fix is approved.

---

## Steps

### 1. Pick the calculator type value

Add the next unused integer to `ScoringCalculatorType` with a `[Display(Name = "...")]` attribute. The display name appears in Site Admin.

### 2. Write the system's recipe

- A scoring system is a `ScoringRecipe` built in `Scoring/Systems/`: one static class per system (or family), whose header comment states the club's rules, and which names a rule for each step. `ScoringPipeline` runs it. See `Systems/Wawasee2026.cs` and `Systems/AppendixA.cs`.

  ```csharp
  public static ScoringRecipe Create(ScoringSystem system) => new()
  {
      Name = "Wawasee 2026",
      RacePoints = LowPointPlaces.AppendixA,
      Adjustments = [new CapAtDefaultScore(), new RacesNeedStartersEachDay(), new AverageFinishOverCountedRaces()],
      Discards = [new UncountedRacesOnly()],
      Total = new Wawasee2026Totals(),
      Eligibility = [],
      Ranking = Ranking.By(new LowestTotal(zeroTotalRanksLast: false)).Then(new LowerAverage())...
  };
  ```

- If an existing recipe already does the maths and only registry options differ (allowed discard methods, columns, heading), reuse it.
- A recipe picks one rule per step, each a small stateless class (the step order is in `ScoringPipeline`'s header):

  | Step | Recipe member | Folder | Examples |
  |---|---|---|---|
  | Race points | `RacePoints` | `Scoring/RacePoints/` | `LowPointPlaces.AppendixA`, `CoxSpraguePoints`, `TopXPoints` |
  | Coded results, instead of race formulas | `CodedResults` (optional) | `Scoring/RacePoints/` | `CodedResultsOneBehindLast` |
  | Score code formulas | `Formulas`, `SeriesFormulas` | `Scoring/Formulas/` | `FormulaSet.Standard`, `FormulaSet.HighPoint` |
  | Adjust scores after formulas | `Adjustments` (in order) | `Scoring/Adjustments/` | `CapAtDefaultScore`, `RaiseToDefaultScore`, `RacesNeedStartersEachDay` |
  | Discards | `Discards` (in order) | `Scoring/Discards/` | `WorstScoresByPattern`, `KeepBestScores`, `UncountedRacesOnly` |
  | Series totals | `Total` | `Scoring/Totals/` | `SumOfScores`, `LowPointAverage`, `Wawasee2026Totals` |
  | Who can be ranked | `Eligibility` (in order) | `Scoring/Eligibility/` | `ParticipationPercent`, `ParticipationOfRacesNotDiscarded` |
  | Ranking | `Ranking` | `Scoring/Tiebreaks/` | `Rankings.LowPoint`, `Rankings.HighPoint` |
  | Code glossary | `Glossary` | `Scoring/Glossary/` | `StandardGlossary`, `TopXGlossary` |

  Also `FixedDefaultScore` (Top X: 0) and `AveragedScores` (which codes count in an average). `Systems/Wawasee2026.cs` and `Systems/TopX.cs` (`Varc2026`) show a whole club system as a list of rules. Reuse a rule, or add one with a parameter; don't change a rule other recipes use.
- Rules get a `ScoringWorkspace` (the run's discard method, fleet place mode, races that don't count) and ask calculator-specific questions through `workspace.Host` (`IScoringHost`).
- Score code formulas (FIX, PLC%, AVE…) are classes in `Scoring/Formulas/`, one per formula. A recipe that scores a formula differently sets `Formulas`, e.g. `Formulas = FormulaSet.Standard.With(new PlacePlusPercentFormula(PlacePlusPercentFormula.RoundingPre2025))`. Don't change a formula class other recipes use: write a new one.
- A new formula clubs can choose: add the class, add it to `FormulaSet.Standard`, and add it to the score code editor's list (`ScoreCodeWithOptionsViewModel.FormulaOptions`). `FormulaSetTests` checks the two lists match.
- Ranking is a chain of tiebreak steps in `Scoring/Tiebreaks/`. Use `Rankings.LowPoint`, `.HighPoint` or `.Pwa`, or build the club's own in the recipe (see `Systems/Wawasee2026.cs`):

  ```csharp
  Ranking = Ranking.By(new LowestTotal(zeroTotalRanksLast: false))
      .Then(new LowerAverage())
      .Then(new AppendixA81LowPoint())
      .Then(AppendixA82.LowPoint);
  ```

  A new tiebreak is a new sealed `ITieBreak` class; don't change one other rankings use.
- Per-run state belongs in `ScoringWorkspace`, not in fields: rules are shared and must stay stateless.
- Rules work on the run's copies of the scores. Read a race's scores with `workspace.ScoresIn(race)` (or `RaceScoresForCounts`), never `Race.Scores`, and don't change the series passed in. Results show `CalculatedScore.Place` and `.Code`.
- Values for an extra results column go in `SeriesCompetitorResults.ColumnValues[key]` (see `Totals/Wawasee2026Totals`), or come from an existing `FlatSeriesScore` property through `valueFromScore` (see `ScoringColumns.NetPoints`).

### 3. Register it

Add a `ScoringCalculatorRegistration` in `ScoringCalculatorRegistry` with `Create = Pipeline(YourSystem.Create)` (use `Simple(...)` when only `Create` is needed):

| Property | Use |
|---|---|
| `Create` | `s => new MyCalculator(s)` |
| `Fields` | `ScoringFieldDescriptor`s for calculator-specific settings shown on the Edit page (label, help, type, required). Types: decimal, integer, percent, and yes/no (`Boolean`, a "Same as parent / Yes / No" select). A setting can be shared by several registrations (see `ScoresToCountField`). |
| `AllowedDiscardMethods` | Defaults to the pattern methods. With exactly one method the dropdown is hidden. |
| `Columns` | Extra `ScoringColumnDescriptor`s after the standard ones (Pre-Discard Total, Participation). Define them in `ScoringColumns`. |
| `TotalHeader` | Heading of the total column (default `"Total"`; PBG uses `"Score"`). |
| `CustomSettingsPartial` | Only for settings the generic field renderer can't handle. |

The Edit page, its client-side script (`Scripts/scoringSystemSettings.ts`), validation (`GetValidationErrors`) and the removal of unused settings on save all come from the registration. They usually need no changes.

### 4. New settings (if any)

Add a **nullable** property (`decimal?`, `int?` or `bool?`) to `ScoringSystemSettings` (null means inherit). `MergeOver`, JSON storage and `KeepOnly` work by reflection. List it in the registration's `Fields`, and read it from `EffectiveSettings`.

### 5. New discard method (rare)

Add it to `SailScores.ApiClient/Enumerations/DiscardMethod.cs` (next integer). Then update:

- `DiscardRaceGroups` for a grouping method, or `ScoringPipeline.ResolveDiscardMethod` for a method that resolves to another one for each series
- `ScoringCalculatorRegistry.UsesDiscardPattern`
- `SeriesService.AddChildSeriesAsSeries`, if it needs child series discards
- `Views/Shared/_DiscardSummary.cshtml` for the results header
- the `data-discard-methods` lists in `Views/ScoringSystem/Create.cshtml` and `Edit.cshtml`

### 6. Results display

Results are flattened in `SeriesService.FlattenResults` and saved as JSON in `HistoricalResults`. Anything new on `FlatResults` or `FlatSeriesScore` must treat null as "saved before this existed". Column visibility and the total heading are saved with the results, so **a series shows changes only after it's recalculated**. The results table (`Views/Shared/_ResultsTable.cshtml`) and CSV export (`Services/CsvService.cs`) render registered columns generically.

### 7. Base system script

Create the base system with a SQL script in `Sql Utility Scripts/`, modeled on `Create VARC 2026 Scoring systems.sql`:

- a fixed `Id` Guid, `ClubId` and `ParentSystemId` null, `CalculatorType`, and defaults for `DiscardPattern`, `DiscardMethod`, `ParticipationPercent` and `Settings`
- skip the insert when a base system with that `CalculatorType` already exists, so the script can run more than once
- copy score codes from the closest existing base system

Run the script on each environment after deploying and applying migrations, as with the PBG and VARC systems.

### 8. Club access

If the system is for specific clubs: Site Admin > Shared Scoring Systems > Access > "Only the selected clubs". Clubs that already use a system as a parent keep it.

### 9. Tests

In `SailScores.Test.Unit/Core/Scoring/`, following `Wawasee2026CalculatorTests.cs` or `PbgCalculatorTests.cs`:

- Run the base system script on local dev and re-export `Fixtures/shared-systems.json` first (see `Core/Scoring/Characterization/README.md`): the scenario builder starts from that base system and its score codes.
- Write scenarios with `Scenarios/Scenario.cs` (the race string format is in its header) and check them with `ScenarioAssert`:

  ```csharp
  var results = Scenario.With(ScoringCalculatorType.AppendixA)
      .Discards("0")
      .Race("A:1 B:2 C:3 D:DNC")
      .NextDay()
      .Race("A:2 B:1 C:ZFP@3 D:4")
      .Score();
  ScenarioAssert.Ranks(results, "B", "A", "C", "D");
  ScenarioAssert.Scores(results, "C", "3 ZFP 4");
  ```

- Reproduce the club's worked example: races, finishes and expected totals, ranks, discards and column values. Ask for one if the rules don't include it.
- Test the registration: the calculator type, columns and allowed discard methods.
- Test validation for any required settings.
- Add the new base system to the characterization tests: re-export `Fixtures/shared-systems.json` and accept the new snapshots (see `Core/Scoring/Characterization/README.md`). Check them against the club's rules.
- Run all unit tests. Existing calculator tests and snapshots must pass unchanged (the golden rule).

Name tests `MethodName_StateUnderTest_ExpectedBehavior`.

### 10. Manual check

1. Run the base system script, then restrict access if needed.
2. As a club admin, create a child system: check the fields shown, inherited placeholders and validation.
3. Score a series with races that exercise the rules; recalculate; check totals, ranks, discards, the results header, extra columns and CSV export.
4. If summary series are involved, score the children first, then the summary.

---

## Checklist

- [ ] `ScoringCalculatorType` value added (next integer, display name)
- [ ] Calculator class added or reused; reads `Effective*` values only
- [ ] Registration in `ScoringCalculatorRegistry` (fields, discard methods, columns, total heading)
- [ ] New settings are nullable properties on `ScoringSystemSettings`
- [ ] No behavior change to existing calculators; existing tests unchanged and passing
- [ ] Unit tests with the club's worked example
- [ ] Base system added to characterization snapshots; existing snapshots unchanged
- [ ] Base system SQL script in `Sql Utility Scripts/` (fixed Id, safe to rerun, score codes copied)
- [ ] Deployment notes: run the script after migrations; set club access in Site Admin
- [ ] Manual check done; series recalculated

## Commands

| Step | Command |
|---|---|
| Build | `dotnet build SailScores.sln --configuration Release` |
| Unit tests | `dotnet test --project SailScores.Test.Unit/SailScores.Test.Unit.csproj` |
| Migration (only for schema changes) | `dotnet ef migrations add <Name> --project SailScores.Database --startup-project SailScores.Web --context SailScoresContext` |
