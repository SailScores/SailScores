# Series Results Display Templates

## Overview

Series Results Display Templates allow each club to control which columns appear in
series results tables and what selected headers should be called.

The feature supports:
- Club-level default templates (one for standard series and one for regatta series)
- Optional per-series template override
- CRUD management of templates for club admins
- Automatic fallback to club defaults when a series has no explicit template

## What can be customized

Each template includes:
- Sail Number visibility
- Competitor Name visibility
- Competitor Name header text (for example, `Helm`)
- Boat Name visibility
- Boat Name header text (for example, `Boat`)
- Competitor Club visibility

- Race columns visibility (the race columns, or child series columns in a summary series).
  When hidden, only competitor and scoring columns (totals, participation) are shown.
- Show Pre-Discard Total
- Show Club Logo
- Highlight top three places (gold, silver and bronze cell colors; on by default)
- Column separators (`ResultsColumnSeparator`): `None`, `EveryColumn` (between every race or
  child series), or `RaceDays` (where the race date changes)

- Score code display, per code (`SeriesResultsTemplateScoreCode`):
  - `ScoreCodeDisplay`: `CodeAndPoints` (default), `CodeOnly` (no points), or `Hidden` (blank cell)
  - Faded: muted like a discard

  Only codes that differ from the default are stored. Codes match ignoring case. These settings
  change display only, never scoring. The series-level "Hide discarded DNC scores" option still
  applies on top of them.

Visibility uses `ColumnVisibility`:
- `Always`
- `OnLargerScreens`
- `Hidden`

## Data model

### New/updated entity fields

- `SeriesResultsTemplate` (new table/entity)
  - `Id`
  - `ClubId`
  - `Name`
  - column visibility/header fields listed above

- `SeriesResultsTemplateScoreCode` (child of a template, cascade delete)
  - `SeriesResultsTemplateId`, `Code` (unique per template), `Display`, `Faded`

- `Club`
  - `DefaultSeriesResultsTemplateId`
  - `DefaultRegattaSeriesResultsTemplateId`

- `Series`
  - `SeriesResultsTemplateId`

### Relationship behavior

Template-related foreign keys are configured with restrictive delete behavior to avoid
SQL Server cascade path conflicts.

This means templates in use by series or club defaults must be detached or replaced
before deletion.

## Default templates and idempotent seeding

`SeriesResultsTemplateService.SeedDefaultTemplatesAsync(clubId)` creates baseline
templates only when a club has no templates.

Default seeded templates:
- `Standard`
  - Competitor Club: `Hidden`
- `Regatta`
  - Competitor Club: `OnLargerScreens`

`EnsureDefaultTemplatesForAllClubsAsync()` runs this logic for clubs missing defaults.

The seeding behavior is idempotent. Re-running it does not duplicate templates for clubs
that already have seeded templates.

## Template resolution behavior

When a series is loaded for results display:
1. If `Series.SeriesResultsTemplateId` is set, that template is used.
2. Otherwise, the club default template is used:
   - Regatta series => `DefaultRegattaSeriesResultsTemplateId`
   - Other series => `DefaultSeriesResultsTemplateId`

This fallback is applied in core series-loading logic so callers receive a resolved
template whenever possible.

## Admin and series workflows

### Club admin

Templates are shown to club admins as "Results views". `SeriesResultsTemplate/Index` is the
single management page, linked from the club settings page (`Admin/Edit`). It holds:
- The club defaults (standard series and regatta), saved by the `SetDefaults` action through
  `ISeriesResultsTemplateService.SetClubDefaultsAsync`, which rejects templates from another club.
- The template list, with badges marking the defaults.

Opening the page seeds the default templates for a club that has none.

`ClubService.UpdateClub` does not write the default template ids, so saving club settings
leaves them alone.

### Series create/edit

Series forms include template selection so each series can optionally override the club
default.

If no template is selected for a series, fallback rules apply at read time.

### Template CRUD

Club admins can manage templates in `SeriesResultsTemplateController`. Edit and delete
return Not Found for a template that belongs to a different club than the one in the route.

Delete protection:
- A template cannot be deleted if it is currently configured as a club default.
- The club default must be changed first.

## Results table rendering

Views build a `ResultsTableViewModel` and call `WithTemplate(resolvedTemplate)` to copy all of the
template's display settings, so a new template setting only needs adding there. The race column
CSS (visibility and the `results-separator` line) comes from `RaceColumnCssClass`.

The results table view model contains explicit template-driven visibility fields.
The legacy `ShowCompetitorClub` behavior is now derived from
`CompetitorClubVisibility` for compatibility.

Export views and API mapping use resolved template visibility rather than old
series-level flags.

## Notes for developers

- Use core services for template/business rules (`ISeriesResultsTemplateService`).
- Keep DB access through core services and `SailScoresContext`.
- Do not manually author EF migration designer or snapshot files.
- If model changes are made, regenerate migrations using EF tooling.

## Related files

- `SailScores.Core/Services/SeriesResultsTemplateService.cs`
- `SailScores.Core/Services/Interfaces/ISeriesResultsTemplateService.cs`
- `SailScores.Web/Controllers/SeriesResultsTemplateController.cs`
- `SailScores.Web/Models/SailScores/SeriesWithOptionsViewModel.cs`
- `SailScores.Web/Services/SeriesService.cs`
- `SailScores.Web/Services/AdminService.cs`
- `SailScores.Database/Entities/SeriesResultsTemplate.cs`
- `SailScores.Database/Entities/Club.cs`
- `SailScores.Database/Entities/Series.cs`
- `SailScores.Database/SailScoresContext.cs`
