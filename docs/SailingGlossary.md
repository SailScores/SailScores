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

All calculators live in `SailScores.Core/Scoring/`, extend `BaseScoringCalculator`, and must be registered in `ScoringCalculatorFactory`. Each calculator corresponds to a sitewide (clubId=null) scoring system.

| System | File | Description |
|--------|------|-------------|
| **Appendix A (Low Point)** | `AppendixACalculator.cs` | ISAF/World Sailing standard: lowest total points wins; place = points; various penalty multipliers for OCS/DQ. The most common system. |
| **Appendix A Pre-2025** | `AppendixAPre2025Calculator.cs` | Same rules but using the penalty values from before the 2025 rule change. |
| **High Point Percentage** | `HighPointPercentageCalculator.cs` | Percentage of possible points earned; higher is better. Used by some inland clubs. |
| **Low Point Ave Excl DNC** | `LowPointAveExclDncCalculator.cs` | Average low-point score, excluding DNC results from the average. |
| **Cox Sprague** | `CoxSpragueCalculator.cs` | Scoring using the Cox-Sprague table. |
| **PWA Standard** | `PwaStandardCalculator.cs` | Used for PWA (windsurfing/kiteboarding) events. |
