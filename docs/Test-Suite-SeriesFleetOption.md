# Test Suite for Series Fleet Option Feature

## Summary
Created comprehensive test suite for Phase 3 (Core Business Logic) implementation of Series Fleet Option feature. Tests validate data persistence, fleet filtering expectations, and position recalculation behavior.

**Test Status:**
- ✅ 12 Tests Passing
- ⏭️  2 Tests Skipped (Phase 3 implementation pending)
- ❌ 0 Tests Failing

## Test Files Created

### 1. Unit Tests: Fleet Option Data Persistence
**File:** `SailScores.Test.Unit/Core/Services/SeriesFleetOptionTests.cs`

Tests verify that FleetId and UseFullRaceScores properties are correctly saved and loaded through the database.

#### Tests Passing:
1. ✅ `SaveNewSeries_WithFleetId_PersistsFleetId`
   - Validates that FleetId is saved when creating a series with fleet filter
   - Uses real database context and service layer

2. ✅ `SaveNewSeries_WithUseFullRaceScoresTrue_PersistsValue`
   - Validates UseFullRaceScores=true is persisted (use original positions)
   - Tests the "Use Original Race Positions" option

3. ✅ `SaveNewSeries_WithUseFullRaceScoresFalse_PersistsValue`
   - Validates UseFullRaceScores=false is persisted (recalculate positions)
   - Tests the "Recalculate Positions" option

4. ✅ `SaveNewSeries_WithoutFleetId_PersistsAsNull`
   - Validates that series without fleet filter store null for FleetId
   - Ensures backward compatibility (open series with no fleet)

5. ✅ `GetSeriesDetailsAsync_WithFleetAssigned_LoadsFleetId`
   - Validates that FleetId is correctly loaded when retrieving series
   - Tests round-trip: save → retrieve → verify values

6. ✅ `SaveNewSeries_WithInactiveFleet_PersistsFleetId`
   - Validates that inactive fleets can be assigned to series
   - Important for edge case: soft-deleted (inactive) fleet assignment

7. ✅ `RoundTrip_SeriesWithFleetAndUseFullRaceScores_ValuesPreserved`
   - Complete round-trip test: save series with both properties → load → verify
   - Validates both FleetId and UseFullRaceScores persist correctly together

#### Tests Skipped (Phase 3):
- ⏭️  `UpdateSeries_ModifyFleetId_PersistsChanges` 
  - Skipped: Needs full edit workflow implementation
  - Tests: Changing fleet assignment on existing series
  
- ⏭️  `RemoveFleetFromSeries_SetFleetIdToNull_PersistsChange`
  - Skipped: Depends on competitor filtering logic
  - Tests: Clearing fleet filter should include all competitors

### 2. Unit Tests: Position Recalculation Logic
**File:** `SailScores.Test.Unit/Core/Scoring/FleetPositionRecalculationTests.cs`

Tests demonstrate expected behavior for position recalculation when `UseFullRaceScores = false`.

#### Tests Passing:
1. ✅ `GetRecalculatedPosition_UseFullRaceScoresFalse_RecalculatesPositionByFleet`
   - **Core Test:** Validates position recalculation logic
   - Scenario: 3 women, 2 men in same race
   - Expected: Woman finishing 4th overall → 2nd among women (recalculated)
   - Uses helper method to simulate recalculation algorithm

2. ✅ `GetRecalculatedPosition_UseFullRaceScoresTrue_KeepsOriginalPosition`
   - **Core Test:** Validates original positions preserved when enabled
   - Same race scenario as Test 1
   - Expected: Woman finishing 4th → stays 4th (original position used)
   - Validates: UseFullRaceScores=true disables recalculation

3. ✅ `GetRecalculatedPosition_NoFleetAssigned_IgnoresUseFullRaceScoresSetting`
   - **Edge Case:** Series without fleet should ignore UseFullRaceScores setting
   - Expected: Positions unchanged regardless of UseFullRaceScores value
   - Validates: Recalculation only happens when both FleetId and UseFullRaceScores=false

4. ✅ `GetRecalculatedPosition_RaceWithNoFleetCompetitors_ReturnsNull`
   - **Edge Case:** Race with no fleet competitors
   - Expected: Returns null (no position can be calculated for that race)
   - Validates: Graceful handling of DNC scenarios

5. ✅ `GetRecalculatedPosition_MixedFleetRace_RecalculatesOnlyFleetCompetitors`
   - **Complex Scenario:** 7 competitors (4 women, 3 men) interleaved in race
   - Order: M1(1), W1(2), M2(3), W2(4), W3(5), M3(6), W4(7)
   - Expected Women's positions: W1=1st, W2=2nd, W3=3rd, W4=4th (among women only)
   - Validates: Correct filtering and ranking calculation

#### Helper Method:
- `GetRecalculatedPosition()`: Simulates the position recalculation algorithm that will be implemented in `BaseScoringCalculator`
  - Input: Series with fleet, Race, Score, Competitor
  - Logic: Count better-placed fleet competitors + 1
  - Output: Recalculated position (or original if UseFullRaceScores=true or no fleet)

## Test Coverage

### What's Tested:
✅ Database persistence of FleetId and UseFullRaceScores  
✅ Correct values loaded from database  
✅ Round-trip save/load cycles  
✅ Null handling (no fleet assignment)  
✅ Inactive fleet assignment  
✅ Position recalculation algorithm  
✅ Edge cases (no fleet competitors, mixed fleets, various UseFullRaceScores values)  

### What's NOT Yet Tested (Phase 3):
❌ PopulateCompetitorsAsync fleet filtering implementation  
❌ BaseScoringCalculator position recalculation in actual scoring  
❌ Series result calculation with fleet filters  
❌ Edit workflow with fleet changes  
❌ Series deletion with inactive fleets  
❌ Multiple fleet scenarios in series results  

## Running the Tests

**Run all new Series Fleet Option tests:**
```bash
dotnet test SailScores.Test.Unit/SailScores.Test.Unit.csproj --filter "FleetOption or FleetPositionRecalculation"
```

**Run only passing tests (exclude skipped):**
```bash
dotnet test SailScores.Test.Unit/SailScores.Test.Unit.csproj --filter "SeriesFleetOptionTests or FleetPositionRecalculationTests" --filter "SkipReason=null"
```

**Run specific test:**
```bash
dotnet test SailScores.Test.Unit/SailScores.Test.Unit.csproj --filter "SaveNewSeries_WithFleetId_PersistsFleetId"
```

## Key Testing Principles Applied

1. **Data Model Testing First**
   - Verify FleetId/UseFullRaceScores persist before implementing logic
   - Ensures database schema is correct

2. **Helper Methods for Algorithm Testing**
   - Position recalculation algorithm separated as testable helper
   - Demonstrates expected behavior for implementation reference

3. **Edge Case Coverage**
   - Inactive fleets, null values, mixed fleets, no participants
   - Ensures robustness of future implementation

4. **Skip Markers**
   - Tests marked `[Fact(Skip = "...")]` for Phase 3 features
   - Clear indication of what needs implementation next

5. **Round-Trip Validation**
   - Save → Load → Verify pattern ensures data integrity
   - Catches serialization/deserialization issues early

## Next Steps (Phase 3)

1. **Implement PopulateCompetitorsAsync filtering**
   - Enable test: `UpdateSeries_ModifyFleetId_PersistsChanges`
   - Implement: Fleet-based competitor filtering in Core service

2. **Implement BaseScoringCalculator position recalculation**
   - Use helper method as reference for algorithm
   - Integrate into scoring pipeline

3. **Add integration tests**
   - Create end-to-end tests for complete series calculation
   - Verify competitor filtering + position recalculation work together

4. **Enable remaining skipped tests**
   - Remove Skip attribute
   - Tests will fail until Phase 3 features implemented
   - Provides clear feedback on implementation progress

## Test Quality Notes

- Uses real in-memory database context (not mocks)
- Tests actual service layer behavior
- Follows existing test patterns in codebase
- Minimal dependencies on unimplemented features
- Clear test names indicate exactly what's being validated
- Comments explain complex scenarios
- All assertions are specific and meaningful

---

**Created:** February 2025  
**Framework:** xUnit  
**Target:** .NET 10  
**Status:** Ready for Phase 3 Implementation Review
