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

- [x] Club admin → Club Settings: the "Result views" section shows a link to the management
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
- [x] Separators "Between child series": in a summary series showing one column per child
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
- [x] Club defaults has "Default View for Summary Series", starting at "Same as standard".
      With it unset, a summary series without its own view uses the standard default; set
      to another view, summary series use that one. The view gets a "Summary default" badge
      and can't be deleted.
- [x] Edit page "Discarded scores": strike through off shows discards without the line;
      faded off shows them in normal text. Existing views still show faded, struck-through
      discards. With strike through and faded off, discards show in parentheses,
      e.g. "(DNC 8)".
- [x] Edit page "Reset to defaults": the form shows the default settings with the explanation
      and keeps the name; Cancel leaves the view unchanged; Save applies the defaults (custom
      fields cleared, score codes all "Code and points").

### F. Scoring columns, totals line and preview

Apply the migration `AddSharedScoringColumnsAndTotalsSeparatorToViews` first. Rebuild so the
CSS and the new script are compiled, then force-refresh.

- [x] After the migration, views of a club whose scoring systems showed Participation or
      Pre-Discard Total (PBGMYS and PBGTEST in the dev data) show the same columns. A view that
      had "Show Pre-Discard Total" on still shows it.
- [x] Edit page "Column visibility" has "Pre-Discard Total" and "Participation"
      (Always / On Larger Screens / Hidden); the old checkbox is gone. Each setting changes the
      series page, regatta page, HTML exports and CSV export. Pre-Discard Total never appears
      for a percent system.
- [x] Scoring system edit page: "Results Columns" no longer lists Pre-Discard Total or
      Participation and links to the results views; calculator columns (e.g. PBG's net
      columns, Wawasee penalties) are still set there.
- [x] "Line between totals and races" (next to Column separators): a line before the first
      race column, with or without other separators; nothing when race columns are hidden.
- [x] Preview (header and next to Save, on both Create and Edit): opens a panel at the bottom
      showing the most recent series; the page still scrolls and the form still works. Changing
      any setting (including typing a header) updates the preview; choosing another series
      shows it. Closing the panel and saving behaves as before; nothing is saved by the preview.
- [x] A club with no series results: the panel says there's nothing to preview.
- [x] With the preview open on a chosen series, "Reset to defaults" reloads the form with the
  panel still open on that series, showing the defaults.
- [x] On a wide screen, a series with seven or fewer races previews at the series page width
  (centered); one with more races uses the full width, as on its series page.
- [x] Club with custom competitor fields turned off: the view create and edit pages have no
  "Custom competitor fields" section. Saving a view that had fields keeps them; turning the
  club setting back on shows them again.

### G. Hide discarded DNC moves to views

Apply the migration `MoveHideDiscardedDncToViews` first. A dry run on the dev data (rolled
back) changed the look of no series: all 101 hiding series now hide through their view.

- [x] Series create, create multiple, edit and summary edit pages no longer have "Hide
      discarded DNC scores". Saving a series doesn't change the old flag (the previous release
      still reads it).
- [x] View edit page, score codes: "Show" has "Hidden when discarded". DNC set to it blanks
      discarded DNCs only; counted DNCs still show. In a percent system, uncounted DNCs are blank.
- [x] After the migration: QCMYC has "New Template (discarded DNC hidden)" used by its 18 hiding
      series; PBGMYS Championship hides discarded DNCs, and its 11 other series use
      "PBGMYS Championship (discarded DNC shown)". IRMSC got Standard and Regatta views, with
      Standard hiding discarded DNCs. Spot-check a hiding and a non-hiding series in each.
- [x] Trend ("Show Rank Trend") is unchanged and still set on the series.

### H. Admin page cleanup

- [x] Club settings: the "Hidden" option reads "Hidden from SailScores front page".
- [x] Club settings no longer has "Default Date Format"; saving club settings keeps the
  club's existing format.
- [x] Results views page, "Race dates": shows the current format and an example for today.
  Typing updates the example (MMMM in May shows "May", not "5ay"); "Use locale default"
  clears it.
- [x] Save a format: race headings on a series page use it straight away. A blank format
  uses the localization. An unsupported one (hh:mm) shows an error and keeps the old one.
- [x] Club settings cards: Club Logo | Visibility & Navigation, Results Display | Competitors,
  Scoring & Localization | Weather. Competitors has the alternative sail number and custom
  field options; turning custom fields on and off still saves straight away and enables
  "Manage custom fields".
- [x] Localization has a note linking to Results Display for the race date format.
- [x] The results views page is titled "Results Display", with "Default views", "Race dates"
  and "Views" sections. Its URL hasn't changed.
- [x] After "Reset to defaults", the notice at the top goes as soon as any setting is changed
  (typing in a header, a checkbox or a select). Choosing a different preview series doesn't
  remove it.
- [x] Preview panel's series list leaves out series with no races. A summary series whose
  child series have races is still listed.
- [x] PBG series, after recalculating: the view's Pre-Discard Total setting shows the column
  (sum of every race score, discards included, beside Net Points). Other average and
  percent systems still don't have it.

### I. Participation races and series info width

- [x] A series with a participation percent, after recalculating: the info under the title
  reads "50% participation (10 races) required for ranking". For PBG the count is after
  discards (half of the races less the most discards a boat could have). Series not yet
  recalculated show only the percent.
- [x] The same text in the series and regatta HTML exports and on the WhatIf page.
- [x] Not signed in (or without edit rights): the series info runs the full width. With the
  New Race panel shown, it wraps beside the panel as before.

### J. Combined columns, alternative sail number, headers and weather

- [x] Run the migration `AddResultsViewSailNumberClubHeadersAndWeather`. Existing views still
  show weather under the race headers, have no alternative sail number column, and show the
  usual Sail and Club headers.
- [x] Edit page: one "Columns" table lists every column with Show and Header. Header boxes for
  Sail Number, Alternative Sail Number and Competitor Club start blank, with the default as
  placeholder; Pre-Discard Total and Participation have no header box. The Alternative Sail
  Number row notes when alternative sail numbers are off in club settings.
- [x] Custom Sail Number, Alt Sail and Club headers appear in the results table and preview;
  clearing one goes back to the default (translated) header. The index list shows custom Sail
  and Club headers under their badges. The regatta competitor list uses them too.
- [x] Alternative sail number column shown (both visibilities): it lists each boat's
  alternative number. In a regatta preferring alternative numbers, the sail number column then
  shows the main number; with the column hidden, it shows the alternative as before.
- [x] Show weather off (help says "where weather was entered"): no wind or weather icons under race headers (normal, abandoned and
  scheduled races). On: shown only for races that have weather.
- [x] Run `AddResultsViewTotalHeaderAndClearDefaultHeaders`. Views that had "Helm" and "Boat"
  now have blank headers: results show "Sailor(s)" and "Boat" in English, translated in other
  club languages. Custom headers (e.g. "Skipper") are kept.
- [x] Edit page: header placeholders show the defaults in the club's language. A Score row
  (always shown) takes a header; blank uses the scoring system's (Total, Score, …). A custom
  total header shows in the results table and preview.
- [x] Series with more than 4 races (repeated column on small screens) and one with 30+
  races (repeated mid-table): with Competitor Name hidden the repeated column is Sail Number,
  then Boat Name, then Alt Sail, with that column's header and values; with all four hidden,
  no repeated columns.
- [x] Pre-Discard Total for percent and average systems: recalculate a series using High
  Point Sum / Top X / Low Point Average / High Point Percentage, turn the column on in its
  view, and check it's the sum of all race scores (Low Point Average without DNCs: DNCs
  count 0). Series not yet recalculated don't have the column.

### K. Results Display page and view form on small screens

- [ ] Results Display page: "Back to Settings" has a back arrow; the default selectors read
  Standard Series / Regattas / Summary Series. The views list has only Name and Actions, with
  no sideways scroll on a phone. Edit uses the pencil icon.
- [ ] Each view lists its shown competitor columns by header (custom headers used, hidden ones
  left out, a desktop icon for "On Larger Screens"). A second line names settings that differ
  from a new view: race columns, Pre-Discard Total, Participation, custom fields shown, some
  scores hidden, some codes restyled, no weather, club logo. A view left at the defaults has
  no second line.
- [ ] View create and edit at phone width: each setting wraps onto its own rows, with no
  tables; section boxes have the same background throughout. From tablet width, Columns has
  Column / Show / Header titles.
- [ ] Help icons open on hover and on a tap, and close on the next tap. Tapping one beside a
  checkbox doesn't tick it. The Alternative Sail Number help mentions the regatta's "Prefer
  Alternative Sail Numbers" (the regatta edit checkbox now uses that wording).
- [ ] Custom field rows: tapping the field name ticks its checkbox; visibility and Order still
  save.
- [ ] Score codes lists only the club's codes: those on its scoring systems and the systems
  they inherit from, plus codes used in its race results. Codes only on unrelated shared
  systems are gone; a code the view already has a setting for is still listed.
- [ ] A club whose weather settings have no wind units: its series pages open, with wind in
  knots. Gusts show the gust, not the wind speed.
