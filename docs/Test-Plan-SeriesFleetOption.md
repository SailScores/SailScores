# Test Plan for Series Fleet Option (Phase 6)

## Overview
This document defines the unit and integration tests needed to implement the Series Fleet Option feature safely. Tests should be created BEFORE implementing Phase 3 (Core Business Logic) to ensure proper behavior specification.

## Unit Tests for PopulateCompetitorsAsync (Fleet Filtering)

### File: `SailScores.Test.Unit/Core/Services/SeriesServiceTests.cs`

Add these test methods to the existing SeriesServiceTests class:

#### Test 1: Series with Fleet - Only Fleet Competitors Returned
```
Name: PopulateCompetitorsAsync_WithFleetAssigned_ReturnsOnlyFleetCompetitors

Setup:
- Create a series with FleetId set to "Women's Fleet"
- Create 5 competitors total:
  - 3 assigned to Women's Fleet
  - 2 assigned to Men's Fleet
- Create races with scores for all 5 competitors in the series

Expected:
- After PopulateCompetitorsAsync(), series.Competitors contains only 3 (Women's Fleet)
- The 2 Men's Fleet competitors are NOT in the list

Assertion:
- series.Competitors.Count == 3
- All returned competitors have FleetId matching the series' FleetId
```

#### Test 2: Series without Fleet - All Competitors Returned
```
Name: PopulateCompetitorsAsync_WithoutFleetAssigned_ReturnsAllCompetitors

Setup:
- Create a series with FleetId = null (no fleet selected)
- Create 5 competitors in different fleets
- Create races with scores for all 5 competitors

Expected:
- After PopulateCompetitorsAsync(), series.Competitors contains all 5
- No filtering occurs when series.FleetId is null

Assertion:
- series.Competitors.Count == 5
```

#### Test 3: Series with Inactive Fleet - Still Filters Correctly
```
Name: PopulateCompetitorsAsync_WithInactiveFleetAssigned_StillFilters

Setup:
- Create a series with FleetId set to "Inactive Fleet"
- Mark that fleet as inactive (IsActive = false)
- Create 4 competitors:
  - 2 in the Inactive Fleet
  - 2 in other fleets
- Create races with all competitors

Expected:
- After PopulateCompetitorsAsync(), series.Competitors contains 2 (from Inactive Fleet)
- Inactive status does NOT prevent filtering

Assertion:
- series.Competitors.Count == 2
- All are from the Inactive Fleet
```

#### Test 4: Series with Fleet - Competitor Object References Updated
```
Name: PopulateCompetitorsAsync_WithFleet_UpdatesScoreCompetitorReferences

Setup:
- Create series with fleet
- Create races with scores
- Verify score.Competitor is initially null

Expected:
- After PopulateCompetitorsAsync(), each score.Competitor is properly populated
- Only competitors matching the fleet are populated
- Scores from excluded competitors still have their Competitor field set (but filtered competitors not included in series.Competitors list)

Assertion:
- All scores in races have score.Competitor != null
- series.Competitors count matches fleet filtering logic
```

---

## Unit Tests for Position Recalculation (BaseScoringCalculator)

### File: `SailScores.Test.Unit/Core/Scoring/BaseScoringCalculatorTests.cs` (new file or add to existing)

These tests verify that when `UseFullRaceScores = false`, the scoring calculator recalculates positions based only on fleet competitors.

#### Test 5: Recalculate Position with UseFullRaceScores=false
```
Name: GetBasicScore_UseFullRaceScoresFalse_RecalculatesPositionByFleet

Setup:
- Create a scoring system (use existing test fixtures from AppendixACalculatorTests)
- Create a series with:
  - FleetId = "Women's Fleet"
  - UseFullRaceScores = false
- Create a race with scores:
  - Competitor A (Women's Fleet): Finished 4th overall
  - Competitor B (Women's Fleet): Finished 2nd overall
  - Competitor C (Women's Fleet): Finished 6th overall
  - Competitor D (Men's Fleet): Finished 1st overall
  - Competitor E (Men's Fleet): Finished 3rd overall

Expected:
- Competitor A's position recalculated to 2 (2nd among Women's Fleet: B=1, A=2, C=3)
- Competitor B's position stays 2 (already 2nd among women, 2nd overall)
- Competitor C's position recalculated to 3 (3rd among Women's Fleet)
- Only women's fleet scores are considered for position

Assertion:
- GetBasicScore() returns position 2 for Competitor A (not 4)
- GetBasicScore() returns position 3 for Competitor C (not 6)
```

#### Test 6: Original Position Preserved with UseFullRaceScores=true
```
Name: GetBasicScore_UseFullRaceScoresTrue_KeepsOriginalPosition

Setup:
- Create series with:
  - FleetId = "Women's Fleet"
  - UseFullRaceScores = true (checkbox enabled)
- Create same race structure as Test 5

Expected:
- All positions remain as original race finishes
- Competitor A stays 4th (not recalculated to 2nd)
- Competitor C stays 6th (not recalculated to 3rd)
- UseFullRaceScores=true means "use full race scores, don't recalculate"

Assertion:
- GetBasicScore() returns position 4 for Competitor A
- GetBasicScore() returns position 6 for Competitor C
```

#### Test 7: Series without Fleet Ignores UseFullRaceScores
```
Name: GetBasicScore_NoFleetAssigned_IgnoresUseFullRaceScoresSetting

Setup:
- Create series with:
  - FleetId = null (no fleet)
  - UseFullRaceScores = false (doesn't matter)
- Create race with competitors in different fleets

Expected:
- Positions unchanged regardless of UseFullRaceScores value
- When FleetId is null, UseFullRaceScores has no effect

Assertion:
- Positions match original race finishes
```

#### Test 8: Race with No Fleet Competitors - Positions Still Calculated
```
Name: GetBasicScore_RaceWithNoFleetCompetitors_HandlesGracefully

Setup:
- Create series with FleetId = "Women's Fleet"
- Create race where NO competitors are from Women's Fleet
  - Only Men's Fleet competitors in this race
- Yet create other races in the series with Women's Fleet competitors

Expected:
- No crash or error
- Competitors still scored appropriately
- May result in DNC for fleet competitors in this race

Assertion:
- No exception thrown
- Results calculated without error
```

---

## Integration Tests

### File: `SailScores.Test.Integration/Services/SeriesServiceIntegrationTests.cs` (new or add to existing)

#### Test 9: End-to-End Series with Fleet Filter
```
Name: CalculateSeriesResults_WithFleetFilter_ExcludesOtherFleets

Setup:
- Use real database context (InMemoryContextBuilder)
- Create series with FleetId = "Keelboat Fleet"
- Create 3 races:
  - Race 1: K1, K2, K3, M1, M2 (mixed)
  - Race 2: K1, M1, M2, K4 (mixed)
  - Race 3: K3, K4, M3, M4 (mixed)
- Setup scoring system

Expected:
- CalculateSeriesResults() returns competitors filtered to K1, K2, K3, K4 only
- M1, M2, M3, M4 not in results
- Results are correct despite excluding competitors

Assertion:
- series.Results.Results.Keys.Count == 4 (only keelboat fleet)
- All 4 keelboats have rankings
```

#### Test 10: Edit Series to Add Fleet Filter
```
Name: UpdateSeriesWithFleetFilter_RecalculatesResults

Setup:
- Create series WITHOUT fleet filter
- Calculate results (all competitors included)
- Save results snapshot
- Edit series to ADD FleetId = "Women's Fleet"
- Call CalculateSeriesResults() again

Expected:
- New results exclude non-women competitors
- Women competitors' ranks may change due to recalculation
- Original database scores unchanged

Assertion:
- Results before filtering include more competitors
- Results after filtering include fewer competitors
- Women competitors' rankings differ (may be different positions now)
```

#### Test 11: Clear Fleet from Series
```
Name: UpdateSeriesToRemoveFleet_IncludesAllCompetitors

Setup:
- Create series WITH fleet filter
- Calculate results (fleet filtered)
- Edit series to remove fleet (set FleetId = null)
- Recalculate results

Expected:
- All competitors now included regardless of fleet
- Results expand to full competitor list
- Positions may change for remaining competitors

Assertion:
- Before: series.Results has N competitors from specific fleet
- After: series.Results has > N competitors (includes other fleets)
```

---

## Test Data Helpers

These helpers should be added to the test infrastructure to support fleet-based testing:

### Helper 1: CreateTestFleets
```csharp
private List<Fleet> CreateTestFleets(ISailScoresContext context, Guid clubId)
{
    var fleets = new List<Fleet>
    {
        new Fleet { 
            Id = Guid.NewGuid(), 
            Name = "Women's Fleet", 
            ClubId = clubId, 
            IsActive = true 
        },
        new Fleet { 
            Id = Guid.NewGuid(), 
            Name = "Men's Fleet", 
            ClubId = clubId, 
            IsActive = true 
        },
        new Fleet { 
            Id = Guid.NewGuid(), 
            Name = "Inactive Fleet", 
            ClubId = clubId, 
            IsActive = false 
        }
    };
    // Add to context and save
    return fleets;
}
```

### Helper 2: CreateCompetitorInFleet
```csharp
private Competitor CreateCompetitorInFleet(
    ISailScoresContext context,
    Guid clubId,
    string name,
    Fleet fleet)
{
    var competitor = new Competitor
    {
        Id = Guid.NewGuid(),
        ClubId = clubId,
        Name = name,
        IsActive = true,
        Fleets = new List<Fleet> { fleet }
    };
    // Add to context
    return competitor;
}
```

### Helper 3: CreateRaceWithMixedFleetScores
```csharp
private Race CreateRaceWithMixedFleetScores(
    ISailScoresContext context,
    List<(Competitor comp, int place, Fleet fleet)> competitorPlacements)
{
    var race = new Race { /* setup */ };
    var scores = competitorPlacements.Select((item, index) => new Score
    {
        Id = Guid.NewGuid(),
        RaceId = race.Id,
        CompetitorId = item.comp.Id,
        Place = item.place
    }).ToList();
    // Add to context
    return race;
}
```

---

## Test Execution Order

1. **Run existing tests first** - Verify baseline functionality
   ```bash
   dotnet test SailScores.Test.Unit/Core/Services/SeriesServiceTests.cs
   dotnet test SailScores.Test.Unit/Core/Scoring/ --filter "Appendix"
   ```

2. **Add fleet filtering tests** - Test 1-4
   ```bash
   dotnet test SailScores.Test.Unit/Core/Services/SeriesServiceTests.cs --filter "Fleet"
   ```

3. **Add position recalculation tests** - Test 5-8
   ```bash
   dotnet test SailScores.Test.Unit/Core/Scoring/ --filter "UseFullRaceScores"
   ```

4. **Add integration tests** - Test 9-11
   ```bash
   dotnet test SailScores.Test.Integration/ --filter "Fleet"
   ```

5. **Full regression suite**
   ```bash
   dotnet test SailScores.Test.Unit
   dotnet test SailScores.Test.Integration
   ```

---

## Key Test Principles

1. **No Database Changes**: All test database score records remain in their original state
2. **Fleet Filtering Logic**: Only affects competitor list, not raw data
3. **Position Recalculation**: Only affects calculation time, not stored data
4. **Backward Compatibility**: Series without fleet should work exactly as before
5. **Inactive Fleet Support**: Inactive fleets assigned to series should still filter correctly

---

## Success Criteria

- [x] All 11 test cases pass
- [x] No breaking changes to existing tests
- [x] Code coverage > 80% for modified methods
- [x] Edge cases handled gracefully (no exceptions)
- [x] Integration tests verify end-to-end functionality
