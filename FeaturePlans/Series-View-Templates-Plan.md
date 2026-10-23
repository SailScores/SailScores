# Series View Templates (issue #504) — Plan

Issue: https://github.com/SailScores/SailScores/issues/504

Series results templates (`SeriesResultsTemplate`) already control which competitor columns
appear. Issue #504 asks for more per-club options for the series view. Changes are per club
(on the template), not per user, and the edit UI stays out of everyday scorekeeping.

## Phase 1 — Consolidate template management

Today the template list/edit pages are at `SeriesResultsTemplate/*`, but the club defaults are
set in the club settings form (`Admin/Edit`).

- `SeriesResultsTemplate/Index` becomes the single management page: club defaults (standard
  series, regatta) at the top, template list below with default badges.
- New `SetDefaults` POST action; core `SetClubDefaultsAsync` checks the templates belong to the
  club.
- `Admin/Edit` keeps a link to the management page and drops the default selectors.
  `ClubService.UpdateClub` no longer writes the default template ids, so saving club settings
  can't clear them.
- Template edit/delete check the template belongs to the club in the route (a club admin could
  previously open another club's template by id).
- Seed default templates when the management page is opened, as `Admin/Edit` does.

## Phase 2 — Display options (top 3, separators, hide race columns)

New template fields (one migration):

- `HighlightTopThree` (bool, default true) — gold/silver/bronze place colors.
- `ColumnSeparators` (enum `ResultsColumnSeparator`: `None`, `EveryColumn`, `RaceDays`) —
  vertical lines between race columns, between race days, or between child series in a
  summary series (each child series is one column, so `EveryColumn`).
- `HideRaceColumns` (bool) — hide race / child series columns, leaving competitor and scoring
  columns.

Work: entity + core model + explicit (non-AutoMapper) conversions, resolved template, edit UI,
`_ResultsTable` / `_ScoreCell` rendering, backup/restore, docs. Collect the duplicated
template-to-`ResultsTableViewModel` copying into one helper used by series, regatta, export
and what-if views.

## Phase 3 — Score code display

Per template, per score code:

- Hidden — cell left blank (generalizes the series-level "Hide discarded DNC").
- Code only — show the code without its points.
- Faded — muted like a discard.

Stored in a child table (`SeriesResultsTemplateScoreCode`: template, code, display mode,
faded). Edit UI lists the codes from the club's scoring systems. Rendering in `_ScoreCell`;
the series `HideDncDiscards` option keeps working. Backup/restore, docs.

## Manual testing checklist

### A. Template management (phase 1)

- [ ] Club admin → Edit Club Info: the "Result views" section shows a link to the management
      page and no default selectors. Saving club settings leaves the defaults unchanged.
- [ ] Management page: change the standard and regatta defaults, save, reload — the badges
      and selectors show the new defaults; a standard series and a regatta use them.
- [ ] Delete is blocked for a default template; works for a non-default one.
- [ ] Open `/{otherClub}/SeriesResultsTemplate/Edit/{id of a template from your club}` —
      returns Not Found.
- [ ] A club with no templates gets Standard and Regatta seeded on opening the page.
