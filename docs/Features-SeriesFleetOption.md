# Add Fleets to Series Implementation Guide
An implemention to address:
https://github.com/SailScores/SailScores/issues/150

## Implementation Status

### ✅ Completed
- [x] Database migration created (`20260223022944_PendingChanges.cs`) - adds `UseFullRaceScores` column to Series table
- [x] Fleet selector UI on Create.cshtml with popover help text
- [x] Fleet selector UI on Edit.cshtml with popover help text
- [x] Fleet selector UI on CreateMultiple.cshtml with popover help text
- [x] "Use Original Race Positions" checkbox UI on all three pages with detailed help
- [x] Field validation and help text explaining fleet filtering behavior
- [x] MultipleSeriesWithOptionsViewModel - added `UseFullRaceScores` property
- [x] CreateVmFromRow in SeriesService - propagates FleetId and UseFullRaceScores to batch-created series
- [x] Checkbox state binding and conditional display (checkbox only shows when fleet is selected)
- [x] Touch-friendly popover help implementation

### 🔄 In Progress / TODO
- [ ] Database entity updates (Series.cs in Database project) - ensure UseFullRaceScores property exists
- [ ] Core model updates (Series.cs in Core project) - ensure UseFullRaceScores is accessible
- [ ] SeriesWithOptionsViewModel - add UseFullRaceScores property
- [ ] Web service (SailScores.Web/Services/SeriesService.cs) - save/load UseFullRaceScores on Create/Edit
- [ ] Core service (SailScores.Core/Services/SeriesService.cs) - implement fleet-based competitor filtering in PopulateCompetitorsAsync()
- [ ] Core scoring (BaseScoringCalculator.cs) - implement position recalculation when UseFullRaceScores=false
- [ ] Controller (SeriesController.cs) - handle fleet and UseFullRaceScores in Create/Edit actions
- [ ] Test coverage - unit and integration tests for fleet filtering and position recalculation
- [ ] Edge cases - handle deleted/inactive fleets, race with mixed fleet competitors, clearing fleet selection

## Overview
Previously, a series was only associated to a fleet indirectly, through the races that were part of
the series. Any competitor in any race that was part of the series would be included in the series
results, regardless of which fleet they were in. When creating a new race, the fleet would default
to the fleet of the most recent race.

This feature adds an _optional_ direct association between a series and a fleet. When a fleet is
selected for the series, then:
- the series results should only include competitors assigned to the selected fleet.
- new races created from the "New Race" button on the should default to the series fleet.

If a fleet is associated with the series, then another series level option is available:
[] Use full race scores. If not selected the race scores will be recalculated to include only
competitors in this fleet.

An example of this feature is a "Women's Series." Races would be added to both the usual series
and the women's series when adding a race. Only the competitors assigned to the "Women's Fleet"
would be included in the series results that is set to the "Women's Fleet." Then, if the second
place woman scored a fourth in the race (but only beaten by one other woman), if this check box
was enabled, she would score "4" and not 2.

## Implementation Plan

### Phase 1: Data Model (🔄 IN PROGRESS)
**Goal:** Ensure database and entity models support UseFullRaceScores

**Tasks:**
1. [ ] Verify `SailScores.Database/Entities/Series.cs` has `UseFullRaceScores` property (bool)
2. [ ] Verify `SailScores.Core/Model/Series.cs` has `UseFullRaceScores` property (bool)
3. [ ] Confirm migration `20260223022944_PendingChanges.cs` is up-to-date with UseFullRaceScores column
4. [ ] Test that database round-trips the UseFullRaceScores value correctly

### Phase 2: Web Service Layer (🔄 TODO)
**Goal:** Web API can save and load fleet and UseFullRaceScores settings

**Tasks:**
1. [ ] Update `SailScores.Web/Services/SeriesService.cs`:
   - [ ] Ensure Create() method saves FleetId and UseFullRaceScores
   - [ ] Ensure Edit() method updates FleetId and UseFullRaceScores
   - [ ] Ensure series details include these fields when loaded
2. [ ] Add `UseFullRaceScores` property to `SeriesWithOptionsViewModel`
3. [ ] Verify round-trip: Save series → Load series → Values match

### Phase 3: Core Business Logic (🔄 TODO)
**Goal:** Implement fleet-based filtering and position recalculation in scoring

**Tasks:**
1. [ ] `SailScores.Core/Services/SeriesService.cs` - Modify `PopulateCompetitorsAsync()`:
   - [ ] When series.FleetId is set, filter competitors to only those with matching FleetId
   - [ ] Document: Raw score records in database are never modified, only result calculation
2. [ ] `SailScores.Core/Scoring/BaseScoringCalculator.cs` - Implement position recalculation:
   - [ ] When UseFullRaceScores = false and series has FleetId:
     - [ ] For each race, count how many fleet competitors scored better
     - [ ] Use that count as the competitor's "place" instead of overall race place
   - [ ] When UseFullRaceScores = true or no fleet: use original race place unchanged

### Phase 4: Controller & View Model Updates (✅ PARTIALLY COMPLETE)
**Goal:** Ensure Create/Edit workflows handle fleet and UseFullRaceScores

**Tasks:**
1. [ ] `SailScores.Web/Controllers/SeriesController.cs`:
   - [ ] Verify Create action loads and passes fleet options to view
   - [ ] Verify Create action saves UseFullRaceScores to database
   - [ ] Verify Edit action loads and passes fleet options to view
   - [ ] Verify Edit action updates UseFullRaceScores in database
2. [ ] `SailScores.Web/Services/SeriesService.cs` (Web layer):
   - [ ] Verify GetSeriesWithOptionsForEditAsync() loads UseFullRaceScores
   - [ ] Verify GetSeriesWithOptionsForEditAsync() loads current FleetId
3. ✅ `SailScores.Web/Models/SailScores/SeriesWithOptionsViewModel.cs`:
   - [ ] Add `UseFullRaceScores` property (bool?)
4. ✅ `SailScores.Web/Models/SailScores/MultipleSeriesWithOptionsViewModel.cs`:
   - [x] Added `UseFullRaceScores` property (bool?)
   - [x] CreateVmFromRow propagates FleetId and UseFullRaceScores to each series

### Phase 5: UI/UX (✅ COMPLETE - Create/Edit, 🔄 TODO - Display)
**Goal:** Users can select fleet and positioning option on series creation/edit, and see fleet info throughout the app

**Create/Edit Tasks:**
- ✅ `SailScores.Web/Views/Series/Create.cshtml`:
  - [x] Fleet dropdown with "Allow any competitors" default option
  - [x] "Use Original Race Positions" checkbox (hidden until fleet selected)
  - [x] Popover help text for both fields
  - [x] Short help text below fields
- ✅ `SailScores.Web/Views/Series/Edit.cshtml`:
  - [x] Same as Create.cshtml
  - [x] Values bound to model properties
- ✅ `SailScores.Web/Views/Series/CreateMultiple.cshtml`:
  - [x] Fleet dropdown with "Allow any competitors" default option
  - [x] "Use Original Race Positions" checkbox (hidden until fleet selected)
  - [x] Popover help text for both fields
  - [x] Short help text below fields
  - [x] CreateVmFromRow applies settings to all created series

**Display/Presentation Tasks (🔄 TODO):**
- [ ] Series Index/List Page:
  - [ ] Series with fleet assigned should be displayed/grouped under that fleet
  - [ ] Fleet name should be visible in series list
  - [ ] Optional: Different styling or icon to indicate fleet-filtered series
- [ ] Series Detail Page:
  - [ ] Add note under series title indicating when series is filtered to a particular fleet
  - [ ] Show fleet name prominently if assigned
  - [ ] Note should be clear: "This series is limited to [Fleet Name] competitors"
- [ ] New Race Button Integration:
  - [ ] When creating new race from a series with a fleet, default the race fleet to the series fleet
  - [ ] If series has a fleet, pre-select it in the "New Race" form
  - [ ] Ensure race creation flow respects series fleet assignment

### Phase 6: Testing (🔄 TODO)
**Goal:** Verify feature works end-to-end with proper test coverage

**Tasks:**
1. [ ] Unit tests for `PopulateCompetitorsAsync()` with fleet filtering:
   - [ ] Test: Series with fleet returns only fleet competitors
   - [ ] Test: Series without fleet returns all competitors
   - [ ] Test: Inactive fleet still works if assigned to series
2. [ ] Unit tests for position recalculation (UseFullRaceScores = false):
   - [ ] Test: Recalculated position matches fleet-only ranking
   - [ ] Test: Original position used when UseFullRaceScores = true
   - [ ] Test: Race with mixed fleet competitors handled correctly
3. [ ] Integration tests:
   - [ ] Create series with fleet filter → verify results exclude other fleets
   - [ ] Edit series to add fleet filter → verify results recalculate
   - [ ] Create multiple series with fleet filter → all inherit settings
   - [ ] Clear fleet from series → results include all competitors again
4. [ ] UI tests:
   - [ ] Checkbox hidden/shown based on fleet selection
   - [ ] Values saved and persisted through edit
   - [ ] Create multiple series applies fleet to all series

### Phase 7: Edge Cases (🔄 TODO)
**Goal:** Handle special scenarios gracefully

**Tasks:**
1. [ ] Inactive fleet assigned to series:
   - [ ] Fleet should be editable and visible in dropdown
   - [ ] Filtering should work regardless of active status
   - [ ] Prevent hard delete of fleets assigned to series
2. [ ] Clearing fleet from series:
   - [ ] Database update successful
   - [ ] Series recalculation includes all competitors
   - [ ] Results display correctly
3. [ ] Race with competitors from multiple fleets assigned to fleet-filtered series:
   - [ ] Only fleet competitors appear in series results
   - [ ] All competitors still visible in race results
   - [ ] Optional: Warning on race edit if series will exclude some racers
4. [ ] No competitors from fleet in a particular race:
   - [ ] Race appears in series with all competitors marked as DNC
   - [ ] Doesn't break series calculation
5. [ ] Deleted fleet (if it happens):
   - [ ] Fall back to filtering all competitors if fleet not found
   - [ ] Log warning or admin alert
   - [ ] Don't break series display

## UI
This will add optional fields under the "Series Details" section when creating or editing a series.
The first will be a dropdown with all active fleets and the selected fleet whether is active or inactive.
It should have a default option for "-Allow any competitors-" which writes a null value to the
database. The Use full race scores option should only be visible if a fleet is selected and
should default to false.

These new options should be available on series creation and edit pages, includeing the
CreateMultipleSeries page.

Possibly: add a warning when a race is assigned to a series with a fleet that will exclude
some competitors from the series results. This could be a warning icon next to the series name, or
a subtle alert message on the page. (Do not disallow this, as it may be desired behavior in some cases.)




## Database Changes
- The Series table already has an optional FleetId column that can be used for this. (Currently only used
  for regatta series, but this feature will be available for all series.)
- Add a new boolean column to the Series table: `UseFullRaceScores`. This will determine whether
  to use the finishing position of the competitor regardless of who is scored in the fleet,
  or to recalculate the finishing position based on only the competitors in the fleet.

## Service Layer

### Overview of Changes
The key principle is: **Race scores in the database are NEVER changed**. Only the calculation of series 
results is affected by fleet selection. The raw Score records remain unchanged; fleet-based filtering 
happens at result calculation time.

### Competitor Filtering for Fleet-Selected Series
When a series has a fleet associated with it:
1. In `SeriesService.PopulateCompetitorsAsync()`: Filter the list of competitors to only include 
   those assigned to the series' fleet.
   - Currently: Gets all competitors who have scores in any race in the series
   - Modified: Additionally filter to only competitors with FleetId matching the series' FleetId
   - This prevents fleet-excluded competitors from appearing in series results at all

### Race Score Recalculation (UseFullRaceScores = false)
When UseFullRaceScores is false and a fleet is selected, position-based scores need recalculation:
1. In `BaseScoringCalculator.GetBasicScore()` or related scoring methods:
   - When a score has a Place value (e.g., 1st, 2nd, 3rd overall)
   - AND UseFullRaceScores is false
   - AND the series has a fleet selected
   - Recalculate the place value to only count competitors from the selected fleet in that race
   - Example: Competitor finished 4th overall but 2nd among fleet members → score as 2

### Current Code Flow for Series Calculation
1. `SeriesService.GetOneSeriesAsync()` loads series with races and scores
2. `SeriesService.CalculateSeriesResults()` calls:
   - `PopulateCompetitorsAsync()` - gets all competitors with scores
   - `ScoringCalculatorFactory.CreateScoringCalculatorAsync()` - creates appropriate scorer
   - `BaseScoringCalculator.CalculateResults(series)` which:
     - Calls `SetScores()` for the scoring algorithm
     - Processes each competitor's individual race scores
     - Calls `CalculateSimpleScores()` to get basic scores per race
     - Calls `CalculateRaceDependentScores()` - processes Place-based scoring
     - Calls `CalculateOverrides()` - applies score code rules
     - Calls `DiscardScores()` - marks discardable scores
     - Calls `CalculateTotals()` - sums non-discarded scores
     - Calls `CalculateRanks()` - ranks competitors by total

### Key Files for Implementation
- `SailScores.Core/Services/SeriesService.cs` - Modify `PopulateCompetitorsAsync()` and potentially create new method for fleet filtering
- `SailScores.Core/Scoring/BaseScoringCalculator.cs` - Modify `GetBasicScore()` or `CalculateSimpleScores()` to recalculate positions
- `SailScores.Core/Model/Series.cs` - Ensure `FleetId` and `UseFullRaceScores` are accessible
- `SailScores.Core/Model/SeriesCompetitorResults.cs` - May need to track fleet-specific scoring info

## Files and Methods to Modify

### Database Layer
- `SailScores.Database/Entities/Series.cs` - Add `UseFullRaceScores` property
- `SailScores.Database/Migrations/` - Create new migration for `UseFullRaceScores` column

### Core Scoring Logic (Race Scores NOT Changed)
- `SailScores.Core/Scoring/BaseScoringCalculator.cs`:
  - Modify `GetBasicScore()` or introduce new method to recalculate position based on fleet
  - May need to pass additional context (Series, FleetId) to scoring methods
  - When UseFullRaceScores = false and series has fleet: recalculate place value to only count fleet competitors
  - When UseFullRaceScores = true: use the original race place value unchanged

### Competitor Filtering
- `SailScores.Core/Services/SeriesService.cs`:
  - Modify `PopulateCompetitorsAsync()` to filter competitors by fleet
    - Only include competitors whose FleetId matches series.FleetId (if set)
    - This prevents fleet-excluded competitors from appearing in series results entirely
  - Modify `CalculateSeriesResults()` or pass fleet context to scoring calculator

### Web Services  
- `SailScores.Web/Services/SeriesService.cs` - Save/update series with fleet and UseFullRaceScores fields
- `SailScores.Web/Services/SeriesListService.cs` - Update series display/filtering logic if needed

### Controllers
- `SailScores.Web/Controllers/SeriesController.cs` - Update Create/Edit actions to handle fleet and UseFullRaceScores

### Views
- `SailScores.Web/Views/Series/Create.cshtml` - Add fleet dropdown and checkbox
- `SailScores.Web/Views/Series/Edit.cshtml` - Add fleet dropdown and checkbox
- `SailScores.Web/Views/Series/CreateMultipleSeries.cshtml` - Add fleet dropdown and checkbox

### Tests
- `SailScores.Test.Unit/Services/SeriesServiceTests.cs` - Add tests for fleet-based competitor filtering
- `SailScores.Test.Unit/Scoring/[ScoringCalculatorTests].cs` - Add tests for position recalculation logic (UseFullRaceScores=false)
- `SailScores.Test.Integration/` - Add integration tests for complete series results with fleets

## Edge Cases

Consider and address the following edge cases:

- **Race with competitors from multiple fleets assigned to a fleet-filtered series:**
  - A race is added to a series filtered for "Women's Fleet", but the race includes male competitors
    In this case, competitors not in the Women's fleet would be excluded from the series results,
    but clicking through to the race results would still show all competitors in the race.
  - During race entry provide a minor warning that one of the selected series will not
    include all competitors from this race.

- **Clearing fleet selection on an existing series:**
  When a fleet is removed (set to null) from a series that previously had fleet-based filtering
  recalculate the series results includind all competitors.

- **Fleet deleted:**
  - A soft delete (fleet set to inactive) should not affect functionality. Make sure that the
    inactive fleet is visible and saved if the series is edited. But inactive fleets should only
    appear in the available options list if they are the currently selected fleet for that series.
  - Attempt to prenent a hard delete in this case: disable the delete button on the admin fleet list.
  - Should the fleet not be found, default to "No value" for the series fleet filter, and include
    all competitors.

- **No competitors in fleet for a race:**
  - A race is part of a series with fleet filtering, but no competitors from that fleet
    participated in the race
  - The race should appear in the series results, but all competitors that have scores in
    other races in the series should have a "DNC".


## Testing
Add Unit tests for the service layer logic that calculates series results with and without a fleet,
and with and without the "Use Full Race Scores" option. Mocking correct race data for these tests
will be key to ensure the logic is working as expected.

Add UI tests to verify the new fields are saving correctly and that the correct competitors are
included in the series results based on the fleet selection. Test both with and without
the "Use Full Race Scores" option selected.

Make sure that clearing the fleet selection on a series that previously had a fleet correctly updates
the database and forces a series recalculation and correctly updates the series results to
include all competitors.

