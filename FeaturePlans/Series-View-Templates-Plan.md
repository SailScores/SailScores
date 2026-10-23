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

## Phase 2 — Display options (top 3, separators, race columns)

New template fields (one migration; existing rows get place colors on and race columns shown):

- `HighlightTopThree` (bool, default true) — gold/silver/bronze place colors.
- `ColumnSeparators` (enum `ResultsColumnSeparator`: `None`, `EveryColumn`, `RaceDays`) —
  vertical lines between race columns, between race days, or between child series in a
  summary series (each child series is one column, so `EveryColumn`).
- `RaceColumnsVisibility` (`ColumnVisibility`, default `Always`) — `Hidden` leaves only the
  competitor and scoring columns; `OnLargerScreens` gives a compact phone view.

Work: entity + core model + explicit (non-AutoMapper) conversions, resolved template, edit UI,
`_ResultsTable` / `_ScoreCell` rendering, docs (backup is phase 4). Collect the duplicated
template-to-`ResultsTableViewModel` copying into one helper used by series, regatta, export
and what-if views.

## Phase 3 — Score code display

Per template, per score code:

- Hidden — cell left blank (generalizes the series-level "Hide discarded DNC").
- Code only — show the code without its points.
- Faded — muted like a discard.

Stored in a child table (`SeriesResultsTemplateScoreCode`: template, code, display mode,
faded), saved with the template; only non-default codes get a row. The edit page lists the
codes from the club's and the shared scoring systems (`IScoringService.GetScoreCodesAsync`),
plus any codes the template already has settings for. Rendering in `_ScoreCell`; the series
`HideDncDiscards` option keeps working. Backup is phase 4.

## Phase 4 — Back up results views

Found while planning: club backups don't include `SeriesResultsTemplates` or their custom
field selections at all, though they do include the club default ids and each series'
template id. Add templates (with the phase 2 and 3 settings) to `ClubBackupData`, and restore
them before clubs and series so their references resolve.

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

### B. Template edit page (phases 1–2)

- [ ] Create a view: the custom competitor fields table now appears on Create (it was only on
      Edit before) and the selections are saved.
- [ ] Edit page has "Race columns", "Highlight top three places" and "Column separators";
      values save and reload. Existing views open with place colors on, race columns Always,
      separators None.
- [ ] Submitting with an empty name re-shows the form with the custom fields table intact.

### C. Results pages (phase 2)

Check on a series page, a regatta page, the series HTML export, the regatta "export all"
HTML, and What If results. Check both light and dark mode.

- [ ] Highlight top three off: no gold/silver/bronze cells; discards still faded.
- [ ] Separators "Between every race": a vertical line before every race column after the
      first, including abandoned/scheduled columns. In a summary series, between child series.
- [ ] Separators "Between race days": lines only where the date changes.
- [ ] Race columns Hidden: only place, competitor columns, total and scoring columns; no repeated
      name column at the right. On Larger Screens: race columns disappear on a phone width.
- [ ] Series HTML export and regatta "export all" now honor "Show Pre-Discard Total" (they
      ignored it before).

### D. Score codes (phase 3)

- [ ] Edit page lists the club's score codes with descriptions, each with "Show" (Code and
      points / Code only / Hidden) and "Faded". Settings save and reload.
- [ ] Hidden DNC: DNC cells are blank on the series page, regatta page and HTML exports; place
      colors don't show in those cells. Points totals are unchanged.
- [ ] Code only DNF: cells show "DNF" (with its tooltip) but no points; discarded ones still
      struck through.
- [ ] Faded DNC: DNC cells are gray like discards, in light and dark mode.
- [ ] A series with "Hide discarded DNC scores" still hides them when the view shows DNC.
- [ ] Deleting a view with score code settings works.

