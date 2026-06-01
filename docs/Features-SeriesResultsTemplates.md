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

Club admin pages expose both default template selectors:
- Default series results template
- Default regatta results template

Template options are loaded from the club's available templates.

### Series create/edit

Series forms include template selection so each series can optionally override the club
default.

If no template is selected for a series, fallback rules apply at read time.

### Template CRUD

Club admins can manage templates in `SeriesResultsTemplateController`.

Delete protection:
- A template cannot be deleted if it is currently configured as a club default.
- The club default must be changed first.

## Results table rendering

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
