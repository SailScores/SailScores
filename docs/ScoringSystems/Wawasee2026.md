# Wawasee 2026 Scoring

## Source

- **Club:** Wawasee Yacht Club
- **Document:** Based on Sailing Instructions published as of October, 2026
- **Link:** [https://www.wawaseeyachtclub.com/sailing-instructions]: the Sailing Instructions, and the Low Point Average Scoring System as a second PDF on the same page
- **Club answers:** the club's answers to the questions in [Assumptions](#assumptions), received 2026-10-19, quoted below under "Club answers"

## Specification

*[SIs that directly affect scoring of WYC series]*

15. **TIME LIMIT** 
15.1 The leading boat must complete the race in one and one-half hours for all racecourses. If no  boat completes the first leg in thirty minutes or three legs in one hour the race shall be  abandoned. Boats failing to finish within thirty (30) minutes after the first boat sails the course  and finishes will be scored the number of boats that finished within the time limit plus two. 
[...]
18. **SCORING** *(See Low Point Average Scoring System for additional detail)* 
18.1 Accredited Race -- At least four or more boats must start the racing day for a race to count.  If one of the participants stops racing for any reason, the remaining three boats can continue to race for the day and have the races count. 
18.2 Scoring Systems -- The Championship Series for each fleet will use the Low Point Average  Scoring System as modified per the participation requirements. All regattas and monthly series  will use the Low Point Scoring System. The Sailscore application will be used to determine the Cynthia Cup award. 
18.3 Excused Absence -- A skipper will receive an excused absence for committee boat duty and  off-the-lake regattas. To receive credit for an off-the-lake regatta, the skipper must be sailing in a  class that is raced at WYC. 
18.4 Eligibility for Awards -- The skipper must compete in at least fifty-one percent (51%) of the  total number of races in the club series to be eligible for awards; excused absences are not  included. The skipper must satisfy the 70% rule in their respective fleet to be eligible for the  Cynthia Cup award. 
18.5 Breakdown Throw-out -- A skipper may request that his boat not be scored due to a  mechanical breakdown on his boat that would prevent him from competitively finishing the race.  The skipper must not finish the race and must notify the race committee or race chairman as soon  as practical. Only ONE breakdown throw-out shall be allowed per boat per seasonal series. The  boat shall receive its seasonal average place for its score. 
18.6 Alternate Skipper -- A skipper may allow another skipper, a crew or an interested party to  skipper his boat and not have his boat scored. The race committee shall be notified prior to the  race that the boat is not to be scored. If the race committee does not receive a notice not to  score and the skipper is a WYC member the boat will be scored. If the Alternate Skipper is a WYC member who owns a boat of the same class, the Alternate Skipper may request the Race  Committee prior to the race to score the Alternate Skipper’s boat. 



**LOW POINT AVERAGE SCORING SYSTEM**


All fleets will use this system for scoring their seasonal series for their fleet championship.  All regattas and monthly series will use the low point scoring system; there will be no throw-outs.

**REQUIREMENTS AND RESTRICTIONS**

Excused Absence -- A skipper will receive an excused absence for committee boat duty and off-the-lake regattas.  To receive credit for an off-the-lake regatta, the skipper must be participating in a class that is raced at WYC.

Boats must sail in (70%) seventy-percent of the total races in the series to avoid penalties; this percentage may vary slightly due to the rounding factor.  Excused absences are included.

Eligibility for Awards -- The skipper must compete in at least (51%) fifty-one percent of the total number of races in the club series to be eligible for awards; excused absences are not included.

Breakdown Throw-out -- A skipper may request that his boat not be scored due to a mechanical breakdown on his boat that would prevent him from competitively finishing the race.  The skipper must notify the race committee or race chairman as soon as practical.  Only ONE breakdown throw-out shall be allowed per boat per seasonal series.  The boat shall receive its seasonal average place for its score. The race shall count toward the fifty-one percent and the seventy-percent rules.  This provision is in addition to and does not replace RRS 62, Redress.

**SCORING**

Boats will receive points equal to their finish in each race in which they participate.
For each race in which they do not participate, boats will receive points equal to their average finish for the series.
For each race less than the required number to be sailed without penalty, boats will receive additional penalty points equal to the number of boats qualifying for awards in the previous year plus one point**.  The required number of races equals 70% of the actual total number of races completed in the series.
Low score wins.  Ties are broken in favor of the boat with the lowest average placement.  When the tie still remains, it shall be broken per RRS Appendix A, para 2.3.

**  See Season Standings Score Sheets for penalty points for each fleet

**CLUB ANSWERS (2026-10-19)**

*[From the club, describing its Season Standings spreadsheet; numbered answers refer to assumptions 1–5 below.]*

We use DNP, Did Not Participate, in place of DNC if a skipper did not race. Normally, the DNC would equal the number of the sailors racing or registered for that series plus 1. This is OK for a set series such as a weekend regatta, but not for our summer season series. The 51% rule and the 70% rule determines their eligibility and required participation without penalties. If you find a DNC in our spreadsheet it is in error.

1) Column R, Average Finish: Equals the sum of the finishes for actual races sailed divided by the total number of actual races sailed to the nearest four decimal places, 0.0000 and rounded two places for display.

2) Column S, Accum Points: Equals the Average Finish times the total number of all races completed in the series to date (shown in cell E5), rounded two places for display.

3) Column V, Accum Penalty Pnts: Equals the number of races a skipper fails to meet the 70% Rule times the Penalty Points designated in cell E6. WYC determined that the penalty points would be equal to the total number of skippers meeting 51% rule plus one from the previous season. A club could use other ways to determine this for their club.

4) Column W, Total Accum Pnts: Equals Column S plus Column V rounded to two places for display.

1. I hope the first paragraph above answers this question.
2. We do not round up to determine actual average, only for display.
3. Again, the first paragraph answers this question.
4. DNS does not apply; DNF = number of starters plus 1; RET = DNF; DSQ = number of starters plus 1. The 20% and 30% penalties are manually calculated and inserted into the spreadsheets.
5. DNS does not count toward the 51% rule. It is a DNP.




---

## Implementation

Everything above this line is the specification, quoted from the club's SIs, scoring document and answers. Everything below
is how SailScores implements it.

- Calculator type: `ScoringCalculatorType.Wawasee2026 = 17`
- Recipe: `SailScores.Core/Scoring/Systems/Wawasee2026.cs` (`Create`)
- Tests: `SailScores.Test.Unit/Core/Scoring/Wawasee2026CalculatorTests.cs`, plus characterization snapshots `Snapshots/SharedSystems/*__Wawasee-2026.verified.json`
- Base system script: `Sql Utility Scripts/Create Wawasee 2026 Scoring system.sql` (base system Id `e41a7c93-5d26-4f0b-8b3e-92c6d1a85f07`)

"SI" is the Sailing Instructions; "LPA" is the club's Low Point Average Scoring System document, cited by its heading; "Club" is the club's answers, by column (R, S, V, W) or number (1–5). The status column compares them with the code as it is (2026-10-19): **implemented**, **differs** (the code does something else; see [Changes the club's answers need](#changes-the-clubs-answers-need)), **kept** (differs, and the maintainer decided not to change it), **not supported** (needs feature work), **out of scope** (not this system's job), or **assumed** (the documents don't say; see [Assumptions](#assumptions)).

### Recipe

```csharp
public static ScoringRecipe Create(ScoringSystem system) => new()
{
    Name = "Wawasee 2026",
    DefaultCode = "DNP",
    RacePoints = LowPointPlaces.AppendixA,
    Adjustments =
    [
        new CapAtDefaultScore(),
        new RacesNeedStartersEachDay(),
        new AverageFinishOverCountedRaces()
    ],
    Discards = [new UncountedRacesOnly()],
    // Includes the participation requirement.
    Total = new Wawasee2026Totals(),
    Eligibility = [],
    Ranking = Ranking.By(new LowestTotal(zeroTotalRanksLast: false))
        .Then(new LowerAverage())
        .Then(new AppendixA81LowPoint())
        .Then(AppendixA82.LowPoint)
};
```

### Clause map

| Rule | What it means here | Implemented by | Pinned by | Status |
|---|---|---|---|---|
| LPA Scoring ¶1 | Race points are finishing places, ties averaged (Appendix A). | `RacePoints/LowPointPlaces.AppendixA` | `CalculateResults_SeasonRanks_LowPointsWin` | implemented |
| SI 15.1 | A boat that doesn't finish within the time limit (TLE) scores the number of boats that finished within it, plus two. | Data: `TLE`, formula `FIN+ 2` (Came to start, Started, not Finished) | `CalculateResults_TimeLimit_ScoresFinishersPlusTwo` | implemented |
| Club ¶1, Club 4 | DNF, RET and DSQ score the starters + 1, not the boats in the series + 1. Starters are boats with a place and no code, or a code that counts as started; a DNS isn't one. | Data: DNF, RET and DSQ are `STR+ 1` (`Formulas/StartersPlusFormula`) | `CalculateResults_DnfRetDsq_ScoreStartersPlusOne` | implemented |
| Club ¶1 | The series + 1 isn't used. SailScores still caps every score at the boats in the series + 1; no club score can reach it, so the cap changes nothing. | `Adjustments/CapAtDefaultScore`, with DNP's Formula Value 1 setting the default score | `CalculateResults_TimeLimit_ScoresFinishersPlusTwo` | implemented |
| SI 18.1 | A day's races count only when at least 4 boats started a race that day; a boat that stops racing doesn't stop the day counting. Otherwise every score that day shows as a discard. | `Adjustments/RacesNeedStartersEachDay` (`MinimumCompetitors` 4), `Discards/UncountedRacesOnly` | `CalculateResults_DayWithThreeBoats_RacesDoNotCount`, `CalculateResults_FourBoatsStartedDuringTheDay_RacesCount` | implemented |
| SI 18.2, LPA intro | The Championship Series (each fleet's seasonal series) uses this system. Regattas and monthly series use plain low point, with no throw-outs: the club gives those series an Appendix A system. | Series setup | Nothing | out of scope |
| SI 18.2, 18.4 | The Cynthia Cup and its 70% eligibility are worked out in a different application, not SailScores. | Nothing | Nothing | out of scope |
| LPA intro | No throw-outs. Only races that don't count (SI 18.1) show as discards. | `Discards/UncountedRacesOnly`; base `DiscardPattern` = `0` | `CalculateResults_DayWithThreeBoats_RacesDoNotCount` | implemented |
| SI 18.3, LPA Excused Absence | Committee boat duty and off-the-lake regattas are excused absences. | Data: the `EXC` code, entered by the scorer | Nothing (data) | implemented |
| LPA Scoring ¶2, SI 18.5, Club ¶1 | A race a boat didn't sail (DNP) scores its average finish for the series; so do EXC and BKD. A boat with no result gets DNP, and so does a code the system doesn't have (a DNC from old data, say). There's no DNC code. | Recipe `DefaultCode = "DNP"`. Data: DNP, EXC and BKD are `AVE` codes. Rule: `Adjustments/AverageFinishOverCountedRaces` | `CalculateResults_NoResultOrDnc_ScoredAsDnp`, `CalculateResults_MissedRaces_ScoreAverageFinishAndPenaltyBelowThreshold`, `CalculateResults_Breakdown_ScoresAverageAndCountsAsStarted`, `CalculateResults_SecondBreakdown_ScoresAsRet` | implemented |
| Club R, Club 3 | The average finish is the boat's points in the races it sailed, over the number of those races: every score in the races that count except DNP, EXC and BKD, so DNF, RET, DSQ and TLE are in. | `Adjustments/AverageFinishOverCountedRaces` | `CalculateResults_MissedRaces_ScoreAverageFinishAndPenaltyBelowThreshold` | implemented |
| Club R, S, Club 2 | The average finish is worked out to 4 decimal places; series points are that average times the races completed. SailScores doesn't round the average, and adds up the race points (the same total, apart from the 4-place rounding). | `Adjustments/AverageFinishOverCountedRaces`, `Totals/Wawasee2026Totals` | `CalculateResults_AverageFinish_NotRounded` | kept (D) |
| Club R, S, W | Average finish and points are shown to 2 decimal places. SailScores shows race scores to 2 places and the total and average columns to 3. | Results views (`_ScoreCell`, `_ResultsTable`, `ScoringColumnExtensions`) | Nothing | differs (E) |
| LPA Requirements ¶2, LPA Scoring ¶3, Club V | A boat that sailed fewer than 70% of the races completed gets penalty points for each race short. Sailed means started, or an excused absence (or BKD); a DNS is scored as a DNP, so it doesn't count (C). The races needed round to the nearest race, a half up, as the club's spreadsheet does (`TRUNC(0.7 * races + 0.5)`; "may vary slightly due to the rounding factor"): 70% of 16 is 11.2, so 11 (G). | `Totals/Wawasee2026Totals` (`PenaltyThresholdPercent` 70) | `CalculateResults_MissedRaces_ScoreAverageFinishAndPenaltyBelowThreshold`, `CalculateResults_ExcusedAbsences_AvoidPenaltyButNotParticipationRequirement`, `CalculateResults_RequiredRacesBelowHalf_RoundDown`, `CalculateResults_RequiredRacesAtHalf_RoundUp`, `CalculateResults_Dns_DoesNotAvoidPenalty` | implemented |
| LPA Scoring ¶3 and note **, Club V | Penalty points are the number of boats qualifying for awards (meeting the 51% rule) the previous season, plus one, set for each fleet. | Data: `PenaltyScoreValue` on each fleet's child system | Nothing (data) | implemented |
| SI 18.4, LPA Eligibility for Awards, Club 5 | To be eligible for awards (ranked), a boat must have competed in 51% of the races. A DNS doesn't count: the club scores it as a DNP. Excused absences don't count; BKD does. | `Totals/Wawasee2026Totals` (`ParticipationPercent` 51); DNS doesn't come to the start | `CalculateResults_Dns_ScoredLikeDnp`, `CalculateResults_ExcusedAbsences_AvoidPenaltyButNotParticipationRequirement`, `CalculateResults_Breakdown_ScoresAverageAndCountsAsStarted` | implemented (C) |
| SI 18.5, LPA Breakdown Throw-out | A boat that breaks down, doesn't finish, and asks not to be scored gets BKD: its average finish. BKD counts toward the 51% and 70% rules. | Data: `BKD` (`AVE`, Came to start, Started, not Finished, Count as participation) | `CalculateResults_Breakdown_ScoresAverageAndCountsAsStarted` | implemented |
| SI 18.5, LPA Breakdown Throw-out | One BKD per boat per seasonal series. A later one scores RET. | Data: a score code group on the base system ("Breakdown throw-outs": BKD, limit 1, overage RET) | `CalculateResults_SecondBreakdown_ScoresAsRet` | assumed (6) |
| SI 18.6 | A boat sailed by an alternate skipper and not scored gets no result: it scores its average finish (as a DNP) and the race counts against its 51% and 70%. | Nothing extra: a boat with no result gets DNP | `CalculateResults_MissedRaces_ScoreAverageFinishAndPenaltyBelowThreshold` | assumed (7) |
| LPA Scoring ¶4, Club W | Series points are race points plus penalties; low score wins. | `Totals/Wawasee2026Totals`, `Tiebreaks/LowestTotal(zeroTotalRanksLast: false)` | `CalculateResults_SeasonRanks_LowPointsWin` | implemented |
| LPA Scoring ¶4 | Ties: the lower average placement wins (the unrounded average finish; see D). | `Tiebreaks/LowerAverage` | `CalculateResults_TiedPoints_LowerAverageFinishWins` | implemented |
| LPA Scoring ¶4 | A tie that remains is broken by "RRS Appendix A, para 2.3", taken as RRS A8. | `Tiebreaks/AppendixA81LowPoint`, then `Tiebreaks/AppendixA82.LowPoint` (RRS A8.1, A8.2) | Covered by the Appendix A tests | assumed (8) |

### Settings and score codes

- **Settings** (base system): `ParticipationPercent` 51; `PenaltyThresholdPercent` 70; `MinimumCompetitors` 4; `DiscardPattern` `0`. `PenaltyScoreValue` is required and set on each fleet's child system.
- **Score code group** (base system): "Breakdown throw-outs": BKD, limited to 1 race per series, latest first; the overage scores RET.
- **Results columns:** Penalties, Average finish, Average with penalties (hidden by default); total header "Points (with Penalties)". An unranked boat's total shows its points with penalties, greyed, not its average (`UnrankedShowsPoints`). Pinned by `Registration_Wawasee2026_HasPenaltyFieldsColumnsAndPointsHeader`, `AverageWithPenaltiesColumn_PointsOverCountedRaces_DividesTotalWithPenalties`.
- **Score codes:** copied from "Appendix A For Regatta" (A5.2), except DNC, which is replaced by DNP, and EXC, BKD and TLE, which are added; DNF, RET and DSQ are then set to `STR+ 1`, and DNS is set like DNP. The club's documents and answers name DNP (a race not sailed), EXC, BKD, the time limit, DNS, DNF, RET, DSQ and the percentage penalties; the rest score as the RRS says (assumption 4).

  | Code | Formula | Discardable | Came to start | Started | Participation | SI says | Status |
  |---|---|---|---|---|---|---|---|
  | DNP | `AVE` (Formula Value 1: sets the cap) | yes | no | no | no | Average finish; doesn't count for 51% or 70% (LPA Scoring ¶2, Club ¶1). Also a missing result. | implemented |
  | DNC | not created: scores as DNP | | | | | Not used (Club ¶1) | implemented |
  | EXC | `AVE` | yes | no | no | **yes** | Average finish; counts for 70%, not 51% (SI 18.3, 18.4) | implemented |
  | BKD | `AVE` | yes | yes | yes | **yes** | Average finish; counts for 51% and 70%; one per series (SI 18.5) | implemented |
  | TLE | `FIN+ 2` | yes | yes | yes | | Finishers within the limit plus two (SI 15.1) | implemented |
  | DNS | `AVE` | yes | no | no | no | Scored as DNP: average finish; doesn't count for 51% or 70% (Club 4, 5) | implemented (C) |
  | OCS, UFD | `SER+ 1` | yes | yes | no | | Not stated: RRS A5.2 | assumed (4) |
  | BFD | `SER+ 1` | **no** | yes | no | | Not stated: RRS A5.2 | assumed (4) |
  | DNF, RET, DSQ | `STR+ 1` | yes | yes | yes | | Starters + 1 (Club 4); RET is also BKD's overage | implemented |
  | NSC | `SER+ 1` | yes | yes | yes | | Not stated: RRS A5.2 | assumed (4) |
  | DNE | `SER+ 1` | **no** | yes | yes | | Not stated: RRS A5.2 | assumed (4) |
  | SCP, DPI, ZFP | `PLC% 20` | yes | yes | yes | | 20% and 30% penalties, worked out by hand (Club 4): the scorer enters the points with RDG (`MAN`) | implemented (F) |
  | TIE, RDG (`MAN`), RDGAve (`AVE`) | as Appendix A | yes | yes | yes | | Not stated: RRS | assumed (4) |

### Changes the club's answers need

Decided by the maintainer, 2026-10-19. Wawasee 2026 isn't on prod yet, so no saved results move.

- **A. DNF, RET and DSQ score the starters + 1** (Club ¶1, 4). Done: a new formula, `STR+`, the
  boats that started (a place with no code, or a code marked Started) plus the Formula Value. A
  DNS isn't a starter. The base system sets DNF, RET and DSQ to `STR+ 1`; BKD's overage, RET,
  follows.
- **B. DNP, not DNC** (Club ¶1). Done: the system's default code is DNP (`ScoringRecipe.DefaultCode`),
  so a missing result, or a code the system doesn't have, is DNP. The base system has DNP (`AVE`,
  Formula Value 1) in place of DNC.
- **C. DNS is a DNP** (Club 4, 5). Done: the base system's DNS code is set like DNP (`AVE`, not
  came to start, not started, not participation), so it scores the average finish and gets no
  credit toward the 51% or 70% rules. This undoes "DNS counts as competing" (assumption 5).
- **D. Round the average finish to 4 decimal places** (Club R, S). Not changing: the average stays
  unrounded. It differs from the club's sheet by less than 0.001, so only an exact tie could
  come out differently.
- **E. Show average finish and points to 2 decimal places** (Club R, S, W). Not changing: the
  total and average columns keep showing 3 places. Display only; scoring isn't affected.
- **F. Percentage penalties** (Club 4). Left as manual entry: the scorer works out the 20% or 30%
  penalty and enters the points with RDG (`MAN`). SCP, DPI and ZFP stay as copied (`PLC% 20`).
- **G. Races needed for 70% round to the nearest race** (club spreadsheet, 2026-10-20). Done:
  the sheet works out races needed as `TRUNC(0.7 * (races completed + races remaining) + 0.5)`,
  so 11.2 is 11 and 3.5 is 4. SailScores had rounded up (11.2 to 12). At the end of the season
  races remaining is 0; SailScores always uses the races that count so far.

### Assumptions

Where the club's documents don't say, or could be read more than one way, SailScores assumed
the following (the maintainer's answers, 2026-10-18). The club answered 1–5 on 2026-10-19; 6–8
are still assumptions.

1. **The cap on scores.** No score is worse than the boats in the series + 1, as in the RRS. DNC scores an average, so its Formula Value of 1 is what sets the cap there. A time limit score (TLE) is capped too; it can't reach the cap, since the boat outside the limit is one of the boats in the series. *Club: the series + 1 isn't used; a race not sailed is a DNP. The cap stays as a safety net, set by DNP's Formula Value; see A and B.*
2. **Average rounding.** The average finish isn't rounded. DNC, EXC and BKD scores, the series points and the "lowest average placement" tiebreak all use the unrounded value. (Other `AVE` codes, such as RDGAve, still round to one decimal, as in the RRS.) *Club: the average is worked out to 4 decimal places and rounded to 2 only for display. SailScores keeps it unrounded (D) and shows 3 places (E).*
3. **What the average finish includes.** Every score in the races that count except the averaged ones (DNC, EXC, BKD): finishes and coded scores such as DNF, RET or TLE. *Club: the finishes in the races actually sailed, so DNP is left out. Read as confirmed: DNF, RET and DSQ have finish points in the club's sheet.*
4. **Codes the documents don't cover** (DNS, DNF, RET, DSQ and the rest) score as the RRS says: one more than the boats in the series (A5.2), and the 20% penalties for SCP, DPI and ZFP. *Club: DNS doesn't apply (it's a DNP); DNF, RET and DSQ score the starters + 1; the 20% and 30% penalties are worked out by hand. See A, C and F. The other codes (OCS, UFD, BFD, DNE, NSC, TIE, RDG) are still assumed.*
5. **"Compete in" (SI 18.4)** means came to the start, so a DNS counts toward the 51%. *Club: a DNS doesn't count; it's a DNP. Changed: see C.*
6. **A breakdown beyond the limit** of one per series scores as RET.
7. **Alternate skipper (SI 18.6).** A boat that isn't scored gets no result, so it gets DNP: average finish, and the race counts against its 51% and 70%.
8. **"RRS Appendix A, para 2.3"** is taken as RRS A8 (A8.1, then A8.2).
