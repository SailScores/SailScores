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
- `ColumnSeparators` (enum `ResultsColumnSeparator`: `None`, `EveryColumn`, `RaceDays`,
  `ChildSeries`) — vertical lines between every column, between race days, or between child
  series in a summary series. A summary can show each child series as one column or show the
  child races, so `ChildSeries` uses the child series of each column
  (`FlatRace.ChildSeriesId`, saved with the results).
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

- [x] Club admin → Edit Club Info: the "Result views" section shows a link to the management
      page and no default selectors. Saving club settings leaves the defaults unchanged.
- [x] Management page: change the standard and regatta defaults, save, reload — the badges
      and selectors show the new defaults; a standard series and a regatta use them.
- [x] Delete is blocked for a default template; works for a non-default one.
- [x] Open `/{otherClub}/SeriesResultsTemplate/Edit/{id of a template from your club}` —
      returns Not Found.
- [x] A club with no templates gets Standard and Regatta seeded on opening the page.

### B. Template edit page (phases 1–2)

- [x] Create a view: the custom competitor fields table now appears on Create (it was only on
      Edit before) and the selections are saved.
- [x] Edit page has "Race columns", "Highlight top three places" and "Column separators";
      values save and reload. Existing views open with place colors on, race columns Always,
      separators None.
- [x] Submitting with an empty name re-shows the form with the custom fields table intact.

### C. Results pages (phase 2)

Check on a series page, a regatta page, the series HTML export, the regatta "export all"
HTML, and What If results. Check both light and dark mode.

- [x] Highlight top three off: no gold/silver/bronze cells; discards still faded.
- [x] Separators "Between every column": a vertical line before every race column after the
      first, including abandoned/scheduled columns.
- [x] Separators "Between race days": lines only where the date changes.
- [ ] Separators "Between child series": in a summary series showing one column per child
      series, a line between each; in a summary showing child races, lines only where the
      child series changes (recalculate an existing summary first). No lines in a standard
      series.
- [x] Race columns Hidden: only place, competitor columns, total and scoring columns; no repeated
      name column at the right. On Larger Screens: race columns disappear on a phone width.
- [x] Series HTML export and regatta "export all" now honor "Show Pre-Discard Total" (they
      ignored it before).

### D. Score codes (phase 3)

- [x] Edit page lists the club's score codes with descriptions, each with "Show" (Code and
      points / Code only / Hidden) and "Faded". Settings save and reload.
- [x] Hidden DNC: DNC cells are blank on the series page, regatta page and HTML exports; place
      colors don't show in those cells. Points totals are unchanged.
- [x] Code only DNF: cells show "DNF" (with its tooltip) but no points; discarded ones still
      struck through.
- [x] Faded DNC: DNC cells are gray like discards, in light and dark mode.
- [x] ~~A series with "Hide discarded DNC scores" still hides them when the view shows DNC.~~
      Replaced by section G: the series option moved to views.
- [x] Deleting a view with score code settings works.

### E. Follow-ups after first testing

Apply the migration `AddDiscardDisplayAndSummaryDefaultView` first. Force-refresh to get
the new CSS.

- [x] Separators are fainter than before in light mode and still visible in dark mode.
- [x] Results Views page: "New Results View" (with the plus icon) sits just above the list,
      not in the page header.
- [ ] Club defaults has "Default View for Summary Series", starting at "Same as standard".
      With it unset, a summary series without its own view uses the standard default; set
      to another view, summary series use that one. The view gets a "Summary default" badge
      and can't be deleted.
- [x] Edit page "Discarded scores": strike through off shows discards without the line;
      faded off shows them in normal text; each color (red, blue, green, orange) shows in
      light and dark mode, lightened when faded is also on. Existing views still show faded,
      struck-through discards. With strike through and faded off and color Normal,
      discards show in parentheses, e.g. "(DNC 8)".
- [ ] Edit page "Reset to defaults": the form shows the default settings with the explanation
      and keeps the name; Cancel leaves the view unchanged; Save applies the defaults (custom
      fields cleared, score codes all "Code and points").

### F. Scoring columns, totals line and preview

Apply the migration `AddSharedScoringColumnsAndTotalsSeparatorToViews` first. Rebuild so the
CSS and the new script are compiled, then force-refresh.

- [ ] After the migration, views of a club whose scoring systems showed Participation or
      Pre-Discard Total (PBGMYS and PBGTEST in the dev data) show the same columns. A view that
      had "Show Pre-Discard Total" on still shows it.
- [ ] Edit page "Column visibility" has "Pre-Discard Total" and "Participation"
      (Always / On Larger Screens / Hidden); the old checkbox is gone. Each setting changes the
      series page, regatta page, HTML exports and CSV export. Pre-Discard Total never appears
      for a percent system.
- [ ] Scoring system edit page: "Results Columns" no longer lists Pre-Discard Total or
      Participation and links to the results views; calculator columns (e.g. PBG's net
      columns, Wawasee penalties) are still set there.
- [ ] "Line between totals and races" (next to Column separators): a line before the first
      race column, with or without other separators; nothing when race columns are hidden.
- [ ] Preview (header and next to Save, on both Create and Edit): opens a panel at the bottom
      showing the most recent series; the page still scrolls and the form still works. Changing
      any setting (including typing a header) updates the preview; choosing another series
      shows it. Closing the panel and saving behaves as before; nothing is saved by the preview.
- [ ] A club with no series results: the panel says there's nothing to preview.
- [ ] With the preview open on a chosen series, "Reset to defaults" reloads the form with the
  panel still open on that series, showing the defaults.
- [ ] On a wide screen, a series with seven or fewer races previews at the series page width
  (centered); one with more races uses the full width, as on its series page.
- [ ] Club with custom competitor fields turned off: the view create and edit pages have no
  "Custom competitor fields" section. Saving a view that had fields keeps them; turning the
  club setting back on shows them again.

### G. Hide discarded DNC moves to views

Apply the migration `MoveHideDiscardedDncToViews` first. A dry run on the dev data (rolled
back) changed the look of no series: all 101 hiding series now hide through their view.

- [ ] Series create, create multiple, edit and summary edit pages no longer have "Hide
      discarded DNC scores". Saving a series doesn't change the old flag (the previous release
      still reads it).
- [ ] View edit page, score codes: "Show" has "Hidden when discarded". DNC set to it blanks
      discarded DNCs only; counted DNCs still show. In a percent system, uncounted DNCs are blank.
- [ ] After the migration: QCMYC has "New Template (discarded DNC hidden)" used by its 18 hiding
      series; PBGMYS Championship hides discarded DNCs, and its 11 other series use
      "PBGMYS Championship (discarded DNC shown)". IRMSC got Standard and Regatta views, with
      Standard hiding discarded DNCs. Spot-check a hiding and a non-hiding series in each.
- [ ] Trend ("Show Rank Trend") is unchanged and still set on the series.

