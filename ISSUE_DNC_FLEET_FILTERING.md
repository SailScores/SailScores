# Issue: DNC and Coded Scores Not Calculating for Fleet-Filtered Series

## Problem
When a series is filtered by fleet (has `FleetId` set), the scoring calculation produces no `CalculatedScores` for the fleet competitors, particularly when those competitors have coded race scores like "DNC" (Did Not Compete).

## Test Evidence
Test: `UpdateSeriesResults_SeriesWithFleetIdAndDNCScores_DNCEquals2`
- Location: `SailScores.Test.Unit\Core\Services\SeriesServiceTests.cs`
- Status: **FAILING**
- Expected: `result.FlatResults.CalculatedScores` contains 2 entries (one for each fleet member)
- Actual: `result.FlatResults.CalculatedScores` is empty (0 entries)

## Test Scenario
1. Two fleets: Fleet A (comp1, comp2) and Fleet B (comp3)
2. Two races: Race 1 in Fleet A, Race 2 in Fleet B
3. Series configured with `FleetId = Fleet A` and `UseFullRaceScores = false`
4. Race 1 scores:
   - comp1: Place 1
   - comp2: Code "DNC"
   - comp3: Code "DNC" (Fleet B member, should be excluded)
5. Race 2 scores:
   - comp3: Code "DNC" (Fleet B member, should be excluded)

## Expected Behavior
- Only comp1 and comp2 (Fleet A members) should have calculated scores
- comp2's DNC should score as 2 (standard DNC penalty)
- comp3's scores should be completely excluded from results

## Actual Behavior
- No calculated scores are generated for anyone
- `result.FlatResults.CalculatedScores` is empty

## Root Cause Analysis - IDENTIFIED ✓

### The Bug
In `PopulateCompetitorsAsync` (SeriesService.cs, line 828), when a score's competitor is not found in the initial fleet-filtered list, the entire `series.Competitors` list is **reassigned**:

```csharp
// Line 828 - THIS IS THE BUG
series.Competitors = _mapper.Map<IList<Competitor>>(dbCompetitors);
```

This creates NEW Competitor object instances. However, earlier in the loop (line 832), `score.Competitor` references were already set to point to the OLD objects from the previous `series.Competitors` list:

```csharp
// Line 832 - now references stale objects
score.Competitor = competitor;
```

### Object Reference Mismatch
Later, in `BaseScoringCalculator.CalculateSimpleScores()` (line 93), scores are matched to competitors using **reference equality**:

```csharp
foreach (var score in scores.Where(s => s.Competitor == comp))
```

**The Problem:** After line 828 reassigns `series.Competitors`, the `comp` objects in the loop don't match the `score.Competitor` references because they're comparing different object instances:
- `comp` = NEW Competitor objects from the reassigned list
- `score.Competitor` = OLD Competitor objects from before the reassignment

Since `==` checks reference equality (not value equality), the match fails and **no scores are added to any competitor's CalculatedScores dictionary**, resulting in empty results.

### Call Flow Demonstrating the Issue
1. `PopulateCompetitorsAsync` loads fleet competitors: `series.Competitors = [Comp1_v1, Comp2_v1]`
2. Assigns `score.Competitor = Comp1_v1` for comp1's score
3. Later finds out comp3's score needs handling, **reassigns**: `series.Competitors = [Comp1_v2, Comp2_v2]` (NEW instances)
4. `CalculateResults` receives series with:
   - `series.Competitors = [Comp1_v2, Comp2_v2]` (NEW)
   - `series.Races[0].Scores[0].Competitor = Comp1_v1` (OLD reference)
5. `CalculateSimpleScores` loops: `foreach (var comp in [Comp1_v2, Comp2_v2])`
6. Filter: `scores.Where(s => s.Competitor == comp)` → Comp1_v1 != Comp1_v2 → **NO MATCH**
7. Result: Empty CalculatedScores

## Files Involved
- `SailScores.Core\Services\SeriesService.cs`
  - `CalculateScoresAsync()` - line 363
  - `PopulateCompetitorsAsync()` - line 753 (LINE 828 IS THE BUG)

- `SailScores.Core\Scoring\BaseScoringCalculator.cs`
  - `CalculateResults()` - line 51
  - `CalculateSimpleScores()` - line 93 (uses reference equality to match scores to competitors)

## Recommended Fix

**Problem Statement:** `series.Competitors` is reassigned (line 828), breaking object references set earlier in the loop.

**Solution:** Instead of reassigning the entire `series.Competitors` list, merge the new competitors into the existing list:

```csharp
// OLD CODE (BUGGY) - Line 828
series.Competitors = _mapper.Map<IList<Competitor>>(dbCompetitors);

// NEW CODE (FIXED) - adds missing competitors instead of replacing
if (dbCompetitors != null && dbCompetitors.Count > 0)
{
    var mappedCompetitors = _mapper.Map<IList<Competitor>>(dbCompetitors);
    foreach (var comp in mappedCompetitors)
    {
        if (!series.Competitors.Any(c => c.Id == comp.Id))
        {
            series.Competitors.Add(comp);
        }
    }
    _cache.Set($"SeriesCompetitors_{series.Id}", dbCompetitors, TimeSpan.FromSeconds(30));
}
```

This preserves the object references already assigned to `score.Competitor` while adding any missing fleet members that appeared only in race scores.

## Additional Test Cases Also Failing
- `UpdateSeriesResults_SeriesWithFleetIdUseFullRaceScoresTrueAndDNCScores_OnlyIncludesFleetCompetitorsWithDNC` - checks with UseFullRaceScores=true
- `UpdateSeriesResults_SeriesWithFleetIdAndMixedCodedScores_OnlyIncludesFleetCompetitors` - checks with mixed codes (DNC, OCS, BFD)
