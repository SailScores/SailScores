# PBGMYS Scoring

## Source

- **Club:** PBGMYS
- **Document:** Emails
- **Specs provided by:** BH, scorekeeper for PBGMYS, October 2026
- **Link:** none (sent by email)
- **File:** none: the specification below is the club's text

## Specification

1. Scores are calculated for a daily series of races and for a Season Summary Series
2. Each class of boats is scored separately, as a one-design fleet.
3. For the daily series (non-summary): each skipper that sails in 6 or more races gets to discard their worst result for that day, except for non-excludable penalties.
4. The same results that are excluded from daily series are also excluded from the summary series' calculations.
5. To be ranked in any series, daily or summary, a competitor must participate in 50% or more of the races in that series. (Participate = any score that indicates the competitor came to the starting area, such as a ranked place, DNS, RET, or DSQ, but not DNC.)
6. The season summary will include any participated races from the daily series for each competitor, regardless of whether the competitor qualifies to be ranked for that day.
7. The score for any series is calculated using an average of the points for any races participated, other than the exclusions mentioned previously. DNC results do not count for this average.
8. The average is calculated to at least three decimal places. (Overriding the Racing Rules of Sailing average calculation to a single decimal place.)
9. Competitors that qualify for ranking are ranked from the lowest to the highest score (low point average.)
10. **View:** The Daily results should display columns for the Average (the final score used for ranking.), the participation percent, the number of races participated, and the Net total (excludes discards.)
11. **View:** The summary series should have an option to not show the individual race results.
12. **View:** The heading information for any series should include the number of races needed to be ranked in that series.
13. Races can be assigned to a daily series. The daily series can be assigned to the appropriate summary series.


---

## Implementation

Everything above this line is the specification, from the club's instructions. Everything below is how SailScores implements it.

- Calculator type: `ScoringCalculatorType.Pbg = 14`
- Recipe: `SailScores.Core/Scoring/Systems/LowPointAverages.cs` (`Pbg`)
- Tests: `SailScores.Test.Unit/Core/Scoring/PbgCalculatorTests.cs`, plus characterization snapshots `Snapshots/SharedSystems/*__PBG.verified.json`
- Base system script: `Sql Utility Scripts/Create PBG Scoring system.sql` (base system Id `b7f3c2d4-6a1e-4f8b-9c05-3e2d8a71f4b6`)

The status column compares the spec with the code as it is (2026-10-18): **implemented**, **differs** (the code does something else), **partly supported**, or **not supported** (needs feature work). PBG isn't on production yet, so a recipe or script change now moves no saved results.

### Recipe

```csharp
public static ScoringRecipe Pbg(ScoringSystem system) => new()
{
    Name = "PBG",
    RacePoints = LowPointPlaces.LowPointAverage,
    AveragedScores = AveragedScores.ExceptCountsNotAtStart,
    Adjustments = [new CapAtDefaultScore(), new ZeroWhenNotAveraged()],
    Discards = [new WorstPercentagesByPattern(lowerScoreWins: true, countAveragedRaces: true)],
    Total = new LowPointAverage(participationAfterDiscards: true),
    Eligibility = [new ParticipationPercent(afterDiscards: true)],
    Ranking = Rankings.LowPoint,
    Glossary = new NotAveragedExcludedGlossary(AveragedScores.ExceptCountsNotAtStart)
};
```

### Clause map

| Spec | What it means here | Implemented by | Pinned by | Status |
|---|---|---|---|---|
| 1, 13 | A daily series holds the day's races; the season summary is a summary series over the daily series, using their races ("child races" rollup). | Series setup: summary series, `SummaryRollupMode.ChildRaces` | `Pbg_SummaryWithChildDiscards_KeepsChildDiscardsAndIgnoresPattern` | implemented |
| 2 | Each class is its own fleet and series; places are finishing places in that fleet, ties averaged. | Series setup (one fleet per series); `RacePoints/LowPointPlaces.LowPointAverage` | Covered by the low point average tests | implemented |
| 3 | Daily series: a boat that sailed 6 or more races discards its worst. "Sailed" counts every race the boat came to the start for (the races in its average). | `Discards/WorstPercentagesByPattern`, with the base system's discard pattern `0,0,0,0,0,1` (the last value repeats) | `Pbg_DailySeries_SixRacesSailedDiscardsWorst`, `Pbg_NonSummarySeries_UsesDiscardPattern` | implemented |
| 3 | Non-excludable penalties (BFD, DNE) are never discarded. If one is a boat's worst score, its next worst is discarded instead. | `WorstPercentagesByPattern` counts the non-excludable race toward the 6, then skips codes with `Discardable = false` when choosing the discard (decision 6) | `Pbg_DailySeries_NonExcludableWorstScore_DiscardsNextWorst` | implemented |
| 4 | The summary discards the same races the daily series did, and applies no pattern of its own. | Discard method `ChildSeriesDiscardsOrPattern` (the only one allowed) | `Pbg_SummaryWithChildDiscards_KeepsChildDiscardsAndIgnoresPattern` | implemented |
| 5 | To rank, a boat must have participated (any code that came to the start, including DNS, RET and DSQ, but not DNC) in 50% of the series' races. | `Eligibility/ParticipationPercent` 50, after discards (decision 1): the races a boat participated in, less its discards, over the series' races less the maximum discards (what a boat sailing every race would discard). Participation is a result, or a code with "Came to start" or "Count as participation". | `Pbg_SummaryParticipation_IsAfterTheMaximumDiscards`, `Pbg_DailySeries_SixRacesSailedDiscardsWorst`, `Pbg_DailySeries_HalfTheRacesWithADiscard_IsShortAfterDiscards` | implemented |
| 6 | The summary includes every race a boat participated in, ranked that day or not. | The summary scores the child series' races directly, so a boat's daily rank doesn't matter. Daily discards are made for every boat, ranked or not. | `Pbg_SummaryWithChildDiscards_KeepsChildDiscardsAndIgnoresPattern` | implemented |
| 7 | The score is the average of the races participated, less discards. DNC (and any code that didn't come to the start) is left out of the points and the race count. | `Totals/LowPointAverage`, `AveragedScores.ExceptCountsNotAtStart`, `Adjustments/ZeroWhenNotAveraged` | `Resolve_PbgColumns_UseAverageNumeratorAndDenominator` | implemented |
| 8 | The series average isn't rounded; results show it to three decimals (`0.###`). | `LowPointAverage` (no rounding); `_ResultsTable.cshtml` format | Snapshots | implemented. RDGAve still rounds to one decimal (decision 2). |
| 9 | Ranked boats go lowest average first; unranked boats follow. | `Tiebreaks/Rankings.LowPoint` | Covered by the low point average tests | implemented |
| (decision 3) | Ties: RRS A8.1, then A8.2. | `Tiebreaks/Rankings.LowPoint` (`AppendixA81LowPoint`, `AppendixA82.LowPoint`) | Covered by the Appendix A tests | implemented |
| 10 | Daily results show columns for the Average, participation percent, races participated, and Net total. | Columns: the total (headed "Score"), Net Points, and Net Series Races. Participation exists but is hidden by default: turn it on in the club's results views. | `CreateCalculator_Pbg_UsesPbgCalculatorWithNetColumnsAndScoreHeader` | **partly supported**: see below |
| 11 | A summary series can hide the individual race columns. | Nothing | Nothing | **not supported** |
| 12 | Each series' heading shows the number of races needed to rank. | The heading shows "50% participation required for ranking" (`Series/Details.cshtml`), not a race count. | Nothing | **not supported** |

### Not supported yet (feature work)

- **Spec 10, columns.**
  - *Average:* shown as the total column, headed "Score" (`TotalHeader` in the registration). Keeping "Score" is fine (decision 5).
  - *Participation percent:* the standard Participation column, hidden by default. Turn it on in the club's results views.
  - *Races participated:* there's no column for it. Net Series Races is the races averaged after discards, which is one fewer than participated once a boat discards. Needs a new column (from the count the participation rule already makes).
  - *Net total:* Net Points, the points after discards. Implemented.
- **Spec 11, hiding race columns in a summary.** Needs a series (or scoring system) option and a change to the results table and exports.
- **Spec 12, races needed in the heading.** The heading shows the percent. Showing the race count needs the count in the results (for example ceil(50% × races), as the participation rule compares) and a change to the heading in the results page, the HTML exports and What-If.

### Settings and score codes

- **Settings** (base system): `ParticipationPercent` 50; `DiscardPattern` `0,0,0,0,0,1`; `DiscardMethod` `ChildSeriesDiscardsOrPattern`.
- **Results columns:** Score (the average), Net Points and Net Series Races; Participation hidden.
- **Score codes:** copied from "Low Point Average without DNCs" (A5.3 style: most codes score `CTS+ 1`). Every code but DNC has "Came to start", so it counts as participation and is averaged, as spec 5 and 7 say.

  | Code | Formula | Discardable | Came to start | Started | Finished | Spec says | Status |
  |---|---|---|---|---|---|---|---|
  | DNC | `SER+ 1` (not averaged) | yes | no | no | no | Not participation; not in the average (5, 7) | implemented |
  | DNS, OCS, UFD | `CTS+ 1` | yes | yes | no | no | Participation (5) | implemented |
  | BFD | `CTS+ 1` | **no** | yes | no | no | Non-excludable (3) | implemented |
  | DNF, NSC | `CTS+ 1` | yes | yes | yes | no | Participation (5) | implemented |
  | RET, DSQ | `CTS+ 1` | yes | yes | yes | yes | Participation (5) | implemented |
  | DNE | `CTS+ 1` | **no** | yes | yes | yes | Non-excludable (3) | implemented |
  | SCP, DPI, ZFP | `PLC% 20` | yes | yes | yes | yes | Available; scoring not specified yet (decision 4) | unconfirmed |
  | TIE, RDG (`MAN`) | as Appendix A | yes | yes | yes | yes | | unconfirmed |
  | RDGAve (`AVE`) | average of other races, rounded to one decimal | yes | yes | yes | yes | Rounds to one decimal (decision 2) | implemented |

  What a code scores (`CTS+ 1`: one more than the boats that came to the start) isn't in the spec.

### Decisions

From the club's spec and the maintainer's answers (2026-10-18).

1. **Discards and participation** (spec 3 and 5). The base system's discard pattern is `0,0,0,0,0,1`. Participation is after discards (changed 2026-10-23, after PBG reached production; it was races sailed over all the series' races): (the boat's races participated − the boat's discards) / (the series' races − the maximum discards). The maximum discards are what a boat sailing every race would discard: the discard pattern for the series' races, or in a summary series, the pattern for each daily series' races, added up (this assumes the daily series use the summary's discard pattern). A boat's own discards in a summary are the ones its daily series made. Examples: 3 days of 8 races, one discard a day: a boat that sails two days (16 races, 2 discards) has 14 of 21. With 12 races and one discard, a boat that sails 6 and discards one has 5 of 11 and isn't ranked.
2. **RDGAve** keeps rounding to one decimal. Spec 8 is about the series average.
3. **Ties:** RRS A8 (A8.1, then A8.2).
4. **Scoring penalties** (SCP, DPI, ZFP) stay available, as copied. How they should score is unspecified for now.
5. **Total column heading:** "Score" is fine; it doesn't need to read "Average".
6. **A non-excludable worst score.** If a boat's worst score is a DNE or BFD, its next worst excludable result is discarded. The DNE or BFD still counts toward the 6 races, so the boat still gets a discard at six races.
