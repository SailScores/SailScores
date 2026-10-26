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
- The standard scoring columns, which every scoring system has: Pre-Discard Total
  (`PreDiscardTotalVisibility`; percent and average systems don't have it, except PBG) and Participation
  (`ParticipationVisibility`). Both hidden by default. Calculator-specific columns
  (penalties, averages, net points) keep their visibility from the scoring system. The rule
  is `ScoringColumns.GetVisibility`, used by the results table and the CSV export.
- Show Club Logo
- Highlight top three places (gold, silver and bronze cell colors; on by default)
- Column separators (`ResultsColumnSeparator`): `None`, `EveryColumn` (between every column),
  `RaceDays` (where the race date changes), or `ChildSeries` (in a summary series, where the
  columns change from one child series to the next; a summary showing child races groups them
  by child series). Each saved column records its child series (`FlatRace.ChildSeriesId`);
  results saved before that show `ChildSeries` lines only for child-series columns, until the
  summary is recalculated. The lines are a little stronger than the row lines, with a
  separate dark mode color.
- Line between totals and races (`TotalsSeparator`): the same line before the first race (or
  child series) column, independent of the column separators setting.
- Discarded scores: strike through and faded (both on by default), and a color
  (`ResultsTextColor`: `Default`, `Red`, `Blue`, `Green`, `Orange`). Faded with a color keeps
  the color, lightened. A faded score code setting still mutes a discarded score. With none
  of the three (no strike through, not faded, normal color), discards are shown in
  parentheses.

- Score code display, per code (`SeriesResultsTemplateScoreCode`):
  - `ScoreCodeDisplay`: `CodeAndPoints` (default), `CodeOnly` (no points), `Hidden` (blank cell),
    or `HiddenWhenDiscarded` (blank only when discarded, or for a DNC that doesn't count in a
    percent system)
  - Faded: muted like a discard

  Only codes that differ from the default are stored. Codes match ignoring case. These settings
  change display only, never scoring. DNC "Hidden when discarded" replaces the series option
  "Hide discarded DNC scores": `Series.HideDncDiscards` is no longer shown or read (saving a
  series leaves it unchanged, for the previous release). The public API's `HideDncDiscards`
  comes from the view. The migration `MoveHideDiscardedDncToViews` moved the setting: each view
  used by a hiding series takes the setting most of its series use, and the other series get a
  copy of the view named "(discarded DNC hidden)" or "(discarded DNC shown)", so no series
  changes how it looks. Clubs with hiding series but no views got the seeded views first.

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
  - `DiscardStrikethrough`, `DiscardFaded` (both default true), `DiscardColor`
  - `PreDiscardTotalVisibility`, `ParticipationVisibility` (default `Hidden`), `TotalsSeparator`
  - `ShowPreDiscardTotal`: no longer read. Saving keeps it in step with
    `PreDiscardTotalVisibility` so the previous release runs on the migrated database; drop it
    in a later release.

- `SeriesResultsTemplateScoreCode` (child of a template, cascade delete)
  - `SeriesResultsTemplateId`, `Code` (unique per template), `Display`, `Faded`

- `Club`
  - `DefaultSeriesResultsTemplateId`
  - `DefaultRegattaSeriesResultsTemplateId`
  - `DefaultSummarySeriesResultsTemplateId` (null: same as the standard series default)

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

The standard scoring columns used to be shown or hidden by scoring system settings
(`ColumnVisibilities`). Seeded templates take the most visible setting from the club's
scoring systems (Always before On Larger Screens), and the migration
`AddSharedScoringColumnsAndTotalsSeparatorToViews` did the same for existing templates
(using `ShowPreDiscardTotal` when no system set the pre-discard total). The old scoring system
values are left in place but no longer used.

The seeding behavior is idempotent. Re-running it does not duplicate templates for clubs
that already have seeded templates.

## Template resolution behavior

When a series is loaded for results display:
1. If `Series.SeriesResultsTemplateId` is set, that template is used.
2. Otherwise, the club default template is used:
   - Regatta series => `DefaultRegattaSeriesResultsTemplateId`
   - Summary series => `DefaultSummarySeriesResultsTemplateId`, or when that is null,
     `DefaultSeriesResultsTemplateId`
   - Other series => `DefaultSeriesResultsTemplateId`

This fallback is applied in core series-loading logic so callers receive a resolved
template whenever possible.

## Admin and series workflows

### Club admin

Templates are shown to club admins as "results views". `SeriesResultsTemplate/Index`, titled
"Results Display", is the single management page, linked from its own card on the club settings
page (`Admin/Edit`). It holds:
- The club defaults (standard series, summary series and regatta), saved by the `SetDefaults`
  action through `ISeriesResultsTemplateService.SetClubDefaultsAsync`, which rejects templates
  from another club. The summary default can be "Same as standard".
- The club's race date format (`Club.DefaultDateFormat`), used in race headings by every
  view. Saved by the `SetDateFormat` action through
  `ISeriesResultsTemplateService.SetClubDateFormatAsync`, which normalizes it with
  `ClubDateFormatUtility` and rejects unsupported formats; the action then clears the web club
  cache. Blank uses the club's localization. Club settings (`Admin/Edit`) no longer show it and
  keep the saved format when they're saved.
- The template list, with badges marking the defaults. A default (of any kind) can't be
  deleted.

The edit page has "Reset to defaults" (`Edit?reset=true`): the form is filled with the default
settings, keeping the name, with no custom fields or score code settings. Nothing is saved
until the form is.

The create and edit pages have a Preview panel (`_PreviewPanel`, a Bootstrap offcanvas that
leaves the page usable). While it is open, `Scripts/seriesResultsTemplatePreview.ts` posts the
form to the `Preview` action on each change. The action shows one of the club's series with
the unsaved settings (`_Preview`, which renders `_ResultsTable`); the panel lists the 30 most
recently updated series with saved results (`GetPreviewSeriesAsync`), the most recent first.
Series from another club aren't shown.

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
