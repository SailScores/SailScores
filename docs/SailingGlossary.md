# Sailing Domain Glossary

Understanding these terms prevents wrong data-model assumptions when working on SailScores.

## Core entities

| Term | Meaning in SailScores |
|------|-----------------------|
| **Club** | Top-level tenant. All data (fleets, series, races, competitors) is scoped to a club. |
| **Season** | A named time window within a club (e.g., "2024 Season"). Series belong to a season. |
| **Fleet** | A grouping of competitors that competes against each other, often by boat class or handicap division. A race belongs to exactly one fleet. |
| **BoatClass** | The design/type of boat (e.g., "Laser", "J/22"). Competitors are associated with a single boat class. |
| **Competitor** | A person (or boat+skipper pair) registered for racing at a club. Has a name, boat name, and boat class. Can be active or inactive which affects visibility in newly created races. |
| **Series** | A set of races scored together over a season. May have a scoring system and drop-race rules. (It will fall back to the club default if not defined at the series level.) Three types: *Standard* (individual races), *Summary* (aggregates child series), *Regatta* (tied to a single fleet). Additional types may be added for some major enhancements. |
| **Race** | A single on-the-water race. Belongs to one fleet. May belong to none, one or more series via the `SeriesRace` junction table. |
| **SeriesRace** | Junction table linking Race ↔ Series. **Always use this table** — never navigate directly from Series to Races. |
| **Score** | A competitor's result in a single race (finish place or a code like DNC). |
| **ScoreCode** | A non-numeric result code: `DNC` (Did Not Come to start), `DNS` (Did Not Start), `DNF` (Did Not Finish), `OCS` (On Course Side at start), `RET` (Retired), `DQ`/`DSQ` (Disqualified), etc. Each code has configurable scoring rules. |
| **ScoringSystem** | The rules used to aggregate per-race scores into a series standing. Assigned at the series or club level; clubs can define custom systems inheriting from the site defaults. |
| **Regatta** | A multi-fleet, multi-series event with a defined start and end date. Has its own series and fleet associations via `RegattaSeries` and `RegattaFleet` junction tables. Can have documents and announcements attached.|
| **Handicap** | A time or points adjustment applied to a competitor. SailScores does not currently supported automatic handicap application, but features towards this goal are being added. |

## Scoring systems

Scoring systems live in `SailScores.Core/Scoring/` (each a recipe in `Scoring/Systems/`, run by `ScoringPipeline`), and must be registered in `ScoringCalculatorRegistry` under a `ScoringCalculatorType`. Each calculator corresponds to a sitewide (clubId=null) base scoring system whose `CalculatorType` column selects it; child systems use their root's type. The registration also lists the calculator's extra settings fields (stored as JSON in `ScoringSystem.Settings`, inherited from parents when null) its allowed discard methods, and any extra results columns it produces (`ScoringColumnDescriptor`; every calculator also gets the standard Pre-Discard Total and Participation columns from `ScoringColumns`). Calculators put column values in `SeriesCompetitorResults.ColumnValues`; the columns and their visibility (`ScoringSystem.Settings` column visibilities, inherited per column) are stored with the flattened results; the standard columns' visibility comes from the results view (`SeriesResultsTemplate`) instead, so `_ResultsTable` and the CSV export show them without per-calculator code. Pattern-based discard methods (by number of races, per day, per child series) split a series' races into groups with `DiscardRaceGroups` and apply the discard pattern to each group; discard rules in `Scoring/Discards/` choose which scores to discard within a group.

| System | File | Description |
|--------|------|-------------|
| **Appendix A (Low Point)** | `Systems/AppendixA.cs` (`Standard`) | ISAF/World Sailing standard: lowest total points wins; place = points; various penalty multipliers for OCS/DQ. The most common system. |
| **Appendix A Pre-2025** | `Systems/AppendixA.cs` (`Pre2025`) | Same rules but using the penalty values from before the 2025 rule change. |
| **High Point Percentage** | `Systems/HighPointPercentages.cs` | Percentage of possible points earned; higher is better. Used by some inland clubs. |
| **Low Point Ave Excl DNC** | `Systems/LowPointAverages.cs` (`ExcludingDnc`) | Average low-point score, excluding DNC results from the average. |
| **Cox Sprague** | `Systems/CoxSprague.cs` | Scoring using the Cox-Sprague table. |
| **PWA Standard** | `Systems/PwaStandard.cs` | Used for PWA (windsurfing/kiteboarding) events. |
| **VARC 2026 Shorthanded** | `Systems/TopX.cs` (`Varc2026`) | Top X High Point race scores (DNC 0); each boat's best `ScoresToCount` scores make the total, and the rest show as discards. Ties: RRS A8.1, then A8.2. |
| **VARC 2026** | `Systems/TopX.cs` (`Varc2026`) | VARC 2026 Shorthanded plus: a race with fewer than `MinimumCompetitors` boats scored (any score but DNC) doesn't count, and `TiesStandAfterA81` can leave ties unbroken after A8.1. |
| **Wawasee 2026** | `Systems/Wawasee2026.cs` | Total of finishing places, with average-coded results (DNC, excused, breakdown) scored at the boat's average finish, plus `PenaltyScoreValue` for each race short of `PenaltyThresholdPercent` (rounded up to a whole race). A day's races count only when `MinimumCompetitors` boats started that day. Ties: lower average finish, then RRS A8. |
| **PBG** | `Systems/LowPointAverages.cs` (`Pbg`) | Low Point Ave Excl DNC. A summary series (child races mode) keeps each child series' discards; other series use the discard pattern. Participation is after discards: the boat's races less its discards, over the series' races less the discards a boat sailing every race would have. Total heading "Score", with Net Points and Net Series Races columns. |
