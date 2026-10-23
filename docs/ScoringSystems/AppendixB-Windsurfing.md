# Appendix B Windsurfing Scoring

Based on [2025-2028-RRS-with-Changes-and-Corrections.pdf](https://media.sailing.org/sailing/wp-content/uploads/2025/07/29083752/2025-2028-RRS-with-Changes-and-Corrections.pdf)



## Specification

Appendix B scoring is based on Appendix A with the following changes.


## APPENDIX B 
## WINDSURFING FLEET RACING RULES 
Windsurfing fleet races (including marathon races) shall be sailed 
under The Racing Rules of Sailing as changed by this appendix. The 
term ‘boat’ elsewhere in the rules means ‘board’ or ‘boat’ as 
appropriate.

[...]
#### 30 STARTING PENALTIES 
Rule 30.2 is deleted. 

[...]

#### B8 CHANGES TO APPENDIX A 
**A1 NUMBER OF RACES; OVERALL SCORES** 
Rule A1 is changed to: 
The number of races scheduled and the number required to be scored to constitute a series shall be stated in the notice of race or sailing instructions; see rule 90.3(a). If an event includes more than one discipline or format, the notice of race or sailing instructions shall state how the overall scores are to be calculated.

**A2 SERIES SCORES** 
Rule A2.1 is changed to: 
Each board’s series score shall, subject to rule 90.3(b), be the total of her race scores excluding her 
(a) worst score when from 5 to 11 races have been scored, or 
(b) two worst scores when 12 or more races have been scored (see rule 90.3(a)). 
However, the notice of race or sailing instructions may make a different arrangement. If a board has two or more equal worst scores, the score(s) for the race(s) sailed earliest in the series shall be excluded. The board with the lowest series score wins and others shall be ranked accordingly.

**A5 SCORES DETERMINED BY THE RACE COMMITTEE** 
Add new rule A5.4: 
A5.4
For an elimination series race that will qualify a board to compete in a later stage of an event, a board that did not sail the course, retired or was disqualified shall be scored points equal to the number of boards permitted to sail in that race. 

**A8 SERIES TIES** 
Rule A8 is changed to: 
A8.1 If there is a series-score tie between two or more boards, each board’s excluded race scores shall be listed in order of best to worst, and at the first point(s) where there is a difference the tie shall be broken in favour of the board(s) with the best excluded race score(s).
A8.2
If a tie remains between two or more boards, each board’s race scores, including excluded scores, shall be listed in order of best to worst, and at the first point(s) where there is a difference the tie shall be broken in favour of the board(s) with the best score(s). These scores shall be used even if some of them are excluded scores.
A8.3
If a tie still remains between two or more boards, they shall be ranked in order of their scores in the last race. Any remaining ties shall be broken by using the tied boards’ scores in the next to-last race and so on until all ties are broken. These scores shall be used even if some of them are excluded scores. 

---

## Implementation

Everything above this line is the specification, quoted from the RRS. Everything below is
how SailScores implements it.

- Calculator type: `ScoringCalculatorType.AppendixBWindsurfing = 18`
- Recipe: `SailScores.Core/Scoring/Systems/AppendixB.cs` (`Windsurfing`)
- New tiebreak: `SailScores.Core/Scoring/Tiebreaks/AppendixBExcludedScores.cs`
- Tests: `SailScores.Test.Unit/Core/Scoring/AppendixBWindsurfingCalculatorTests.cs`, plus characterization snapshots `Snapshots/SharedSystems/*__Appendix-B-Windsurfing.verified.json`
- Base system script: `Sql Utility Scripts/Create Appendix B Windsurfing Scoring system.sql` (base system Id `bcf8fd64-39a6-44dc-aa4a-5c1607382c8a`)

Every row is pinned by the tests named in it. A row is **unconfirmed** until it has been reviewed against the spec.

### Recipe

The same as `AppendixA.Standard` except for the ranking:

```csharp
public static ScoringRecipe Windsurfing(ScoringSystem system) => new()
{
    Name = "Appendix B Windsurfing",
    RacePoints = LowPointPlaces.AppendixA,
    Adjustments = [new CapAtDefaultScore()],
    Discards = [new WorstScoresByPattern()],
    Total = new SumOfScores(),
    Eligibility = [new ParticipationPercent()],
    Ranking = Ranking.By(new LowestTotal(zeroTotalRanksLast: true))
        .Then(new AppendixBExcludedScores())
        .Then(new PwaBetterScoresIncludingDiscards())
        .Then(AppendixA82.LowPoint)
};
```

### Clause map

| Rule (spec) | What it means here | Implemented by | Pinned by |
|---|---|---|---|
| Preamble: "boat" means "board" | Wording only. Results pages keep saying "boat". | Nothing | Nothing |
| Rule 30.2 deleted | No Z flag, so no ZFP penalty. | Data: the base system has no `ZFP` score code (decision 2). | `BaseSystem_AppendixBWindsurfing_ScoresA52WithoutZfp` |
| Other `[...]` changes in Appendix B | They affect which code a board is given, not how that code scores (decision 3). | Nothing | Nothing |
| B8 A1: number of races, overall scores | Set in the NOR/SIs. SailScores doesn't enforce a minimum number of races. Overall scores for several disciplines can use a summary series. | Nothing (out of scope) | Nothing |
| B8 A2.1: exclude the worst score from 5 to 11 races, the two worst from 12 | Discard pattern by number of races scored. The last value in the pattern repeats, so it covers 12 races and more. | Data: base `DiscardPattern` = `0,0,0,0,1,1,1,1,1,1,1,2`. Rule: `Discards/WorstScoresByPattern` (reused) | `CalculateResults_RaceCount_ExcludesByAppendixBSchedule` (4, 5, 11, 12 and 15 races) |
| B8 A2.1: "subject to rule 90.3(b)" | Some scores can't be excluded. | Data: score codes with `Discardable = false` (`DNE`, `BFD`), copied from Appendix A. Rule: `WorstScoresByPattern` skips them. | `CalculateResults_DneIsWorstScore_NextWorstDiscarded`, `BaseSystem_AppendixBWindsurfing_ScoresA52WithoutZfp` |
| B8 A2.1: "the NOR or SIs may make a different arrangement" | A club changes the discards on its own child system. | Club child systems override `DiscardPattern` and `DiscardMethod`. The registration keeps the default `PatternDiscardMethods`. | `CalculateResults_ChildSystemDiscardPattern_Overrides` |
| B8 A2.1: equal worst scores, so the earliest race is excluded | Decides which race shows as the discard. It can't change totals, and it can't change ranks because B A8.1 compares values, not races. | `WorstScoresByPattern` (reused): it orders equal scores by `Race.Date`, then `Race.Order`. | `WorkedExample1_EqualWorstScores_EarliestRaceDiscarded` |
| B8 A2.1: lowest series score wins | Sum of the scores not excluded; lowest total ranks first. | `Totals/SumOfScores`, `Tiebreaks/LowestTotal(zeroTotalRanksLast: true)` (reused). The zero-total quirk can't occur because first place scores 1. | Worked examples 1 to 3 |
| A4, A5.1 to A5.3 (unchanged by Appendix B) | Places are points, ties are averaged, and codes score by their formula with the 2025 rounding. No score is worse than DNC. The base system's codes score by A5.2; a club can switch to A5.3 (see [Using A5.3](#using-a53-in-a-club-system)). | `LowPointPlaces.AppendixA`, `FormulaSet.Standard`, `Adjustments/CapAtDefaultScore`, all as in Appendix A (reused) | Covered by the Appendix A tests; here `CalculateResults_BaseCodes_ScoreA52` |
| B8 A5.4 (new): elimination series races | SailScores doesn't model elimination series or the number of boards allowed in a race. | Nothing (out of scope, decision 4) | Nothing |
| B8 A8.1: excluded scores, best to worst | First tiebreak. Compare **only** the excluded scores, best first; at the first difference the better one wins. If the boards excluded different numbers of scores, compare only as far as the shorter list goes; no difference there means the tie goes on to A8.2 (decision 1). This replaces Appendix A's A8.1, which uses the scores that weren't excluded. | New `Tiebreaks/AppendixBExcludedScores`. **Not** `PwaBetterDiscard`, which ranks the board with more discards ahead when the lists match as far as the shorter one goes. | `WorkedExample1_TieOnTotal_BetterExcludedScoreRanksAhead`, `WorkedExample1_UnderAppendixA_CountedScoresDecideTheOtherWay`, `ExcludedScores_SameAsFarAsShorterList_Ties`, `ExcludedScores_DifferenceWithinShorterList_BetterRanksAhead`, `ExcludedScores_NoneExcluded_Ties` |
| B8 A8.2: all scores including excluded ones, best to worst | Second tiebreak. | `Tiebreaks/PwaBetterScoresIncludingDiscards` (reused; its comparison is the same rule). Its off-by-one quirk can't trigger because every board has a score in every race. | `WorkedExample2_SameExcludedScores_BetterScoresIncludingExcludedRankAhead` |
| B8 A8.3: last race, then the race before, excluded scores included | Third tiebreak. | `Tiebreaks/AppendixA82.LowPoint` (reused) | `WorkedExample3_NoExcludedScoresAndSameScores_LastRaceDecides`, `Tie_LastRaceExcluded_StillDecides` |

### Settings and score codes

- **Settings:** none specific to this system. Registration is `Simple(ScoringCalculatorType.AppendixBWindsurfing, Pipeline(AppendixB.Windsurfing))`.
- **Base system:** one, "Appendix B Windsurfing" (decision 5). Defaults: `DiscardPattern` `0,0,0,0,1,1,1,1,1,1,1,2`; `DiscardMethod` null (race count pattern); `ParticipationPercent` null, as in the Appendix A base systems.
- **Score codes:** copied from "Appendix A For Regatta", except `ZFP`. Those codes score by A5.2, which the RRS applies unless the NOR says A5.3 applies. Every code that ends a board's race scores `SER+ 1`: one more than the boards in the series.

  | Code | Formula | Discardable | Note |
  |---|---|---|---|
  | DNC, DNS, OCS, UFD, DNF, RET, DSQ, NSC | `SER+ 1` | yes | |
  | BFD, DNE | `SER+ 1` | **no** | 90.3(b) |
  | SCP, DPI | `PLC% 20` | yes | |
  | TIE, RDG (`MAN`), RDGAve (`AVE`) | as Appendix A | yes | |
  | ZFP | not created | | Rule 30.2 is deleted |

### Using A5.3 in a club system

Under A5.3, a board that came to the starting area but didn't start, didn't finish, retired or was disqualified scores one more than the number of boards that came to the starting area. A board that didn't come to the starting area still scores one more than the boards in the series.

A club's child system can have its own copy of a parent's score code, and that copy replaces the parent's code of the same name (`ScoringService` leaves out inherited codes whose name the child already uses). To apply A5.3, the club creates a child system of "Appendix B Windsurfing" and adds these codes, each with formula **`CTS+`** and value **1**. Every other field stays as in the parent, including **Discardable: no** for `BFD` and `DNE`:

| Code | Came to start | Started | Finished | Discardable |
|---|---|---|---|---|
| DNS | yes | no | no | yes |
| OCS | yes | no | no | yes |
| UFD | yes | no | no | yes |
| BFD | yes | no | no | **no** |
| DNF | yes | yes | no | yes |
| RET | yes | yes | yes | yes |
| DSQ | yes | yes | yes | yes |
| DNE | yes | yes | yes | **no** |
| NSC | yes | yes | no | yes |

`DNC` keeps `SER+ 1` from the parent. This is the same set of differences as between the two Appendix A base systems ("For Regatta" and "For Series - Rule 5.3"). The tiebreaks and discards don't change.

Pinned by: `CalculateResults_ChildSystemCtsCodes_ScoresA53`.

### Worked examples

These are built examples, not official ones (decision 6). Each one has five races, so each board excludes one score, except example 3, which has four races and no exclusions. Excluded scores are in brackets. Races are written in the `Scenario` format, so they can go straight into tests.

#### Example 1: B A8.1, the better excluded score wins

```
R1: B:1 A:2 C:3 D:4 E:5
R2: B:1 A:2 D:3 E:4 C:5
R3: C:1 A:2 B:3 E:4 D:5
R4: C:1 D:2 A:3 B:4 E:5
R5: D:1 E:2 C:3 A:4 B:5
```

| Board | R1 | R2 | R3 | R4 | R5 | Total | Rank |
|---|---|---|---|---|---|---|---|
| C | 3 | (5) | 1 | 1 | 3 | 8 | 1 |
| A | 2 | 2 | 2 | 3 | (4) | 9 | 2 |
| B | 1 | 1 | 3 | 4 | (5) | 9 | 3 |
| D | 4 | 3 | (5) | 2 | 1 | 10 | 4 |
| E | (5) | 4 | 4 | 5 | 2 | 15 | 5 |

- A and B tie on 9. By B A8.1, A's excluded 4 beats B's excluded 5, so **A ranks ahead**.
- Appendix A's A8.1 would decide this tie the other way: B's counted scores, best first (1, 1, 3, 4), beat A's (2, 2, 2, 3) at the first point. This example is what separates the two systems.
- E scores 5 in R1 and R4, and the earlier race, R1, is excluded (B A2.1).

Expected ranks: C, A, B, D, E.

#### Example 2: B A8.2, equal excluded scores, then all scores

```
R1: A:1 C:2 B:3 D:4 E:5
R2: C:1 B:2 A:3 D:4 E:5
R3: D:1 B:2 A:3 C:4 E:5
R4: E:1 C:2 B:3 D:4 A:5
R5: E:1 D:2 A:3 C:4 B:5
```

| Board | R1 | R2 | R3 | R4 | R5 | Total | Rank |
|---|---|---|---|---|---|---|---|
| C | 2 | 1 | (4) | 2 | 4 | 9 | 1 |
| A | 1 | 3 | 3 | (5) | 3 | 10 | 2 |
| B | 3 | 2 | 2 | 3 | (5) | 10 | 3 |
| D | (4) | 4 | 1 | 4 | 2 | 11 | 4 |
| E | (5) | 5 | 5 | 1 | 1 | 12 | 5 |

- A and B tie on 10. Both excluded a 5, so B A8.1 doesn't decide.
- By B A8.2, all scores best first: A has 1, 3, 3, 3, 5 and B has 2, 2, 3, 3, 5. A's 1 beats B's 2, so **A ranks ahead**.
- C and D each have two equal worst scores, and the earlier one is excluded (C: R3, not R5; D: R1, not R2 or R4).

Expected ranks: C, A, B, D, E.

#### Example 3: B A8.3, no excluded scores and the same scores

```
R1: A:1 B:2 C:3 D:4
R2: B:1 A:2 C:3 D:4
R3: A:1 B:2 D:3 C:4
R4: B:1 A:2 D:3 C:4
```

| Board | R1 | R2 | R3 | R4 | Total | Rank |
|---|---|---|---|---|---|---|
| B | 2 | 1 | 2 | 1 | 6 | 1 |
| A | 1 | 2 | 1 | 2 | 6 | 2 |
| D | 4 | 4 | 3 | 3 | 14 | 3 |
| C | 3 | 3 | 4 | 4 | 14 | 4 |

- Four races, so there are no exclusions. B A8.1 has nothing to compare, and the tie goes on.
- B A8.2: A and B both have 1, 1, 2, 2, and C and D both have 3, 3, 4, 4, so it doesn't decide either tie.
- B A8.3, the last race: B's 1 beats A's 2, and D's 3 beats C's 4. **B ranks ahead of A, and D ranks ahead of C.**

Expected ranks: B, A, D, C.

### Decisions

The questions raised while mapping the spec, with the answers given (2026-10-18).

1. **B A8.1 with different numbers of excluded scores.** Two tied boards can exclude different numbers of scores, for example when only one of them has a non-discardable code and has nothing else to exclude. The spec doesn't say what happens.
   **Answer:** yes, compare as far as the shorter list goes. This should be a very rare case: after enough races for a discard, almost all competitors will have race scores that can be excluded.
2. **ZFP.** Should the base system leave it out, as rule 30.2 is deleted?
   **Answer:** yes, ZFP should not be included in the scoring codes for this system.
3. **Sections the spec leaves out (`[...]`).** Does any of the rest of Appendix B affect scoring?
   **Answer:** The other changes don't affect scoring results. They may affect when competitors are assigned particular codes, but after that there is not an effect on the scoring.
4. **A5.4, elimination series.** Is this out of scope?
   **Answer:** Elimination series are out of scope for this; this work includes the scoring system.
5. **One base system or two?**
   **Answer:** One base scoring system. Please add a note to this doc for how appendix A can be implemented by a club, by setting scoring code overrides for the appropriate codes. (See [Using A5.3 in a club system](#using-a53-in-a-club-system).)
6. **Worked example.** Is there an official or club example?
   **Answer:** We don't currently have an authoritative worked example. Create one for now. Some day we will add an real world example/test.
7. **A5.2 or A5.3 for the base system's codes?**
   **Answer:** A5.2 is the base; A5.3 stays a club override (see [Using A5.3 in a club system](#using-a53-in-a-club-system)). If many clubs want A5.3, it can become a separate base system.
