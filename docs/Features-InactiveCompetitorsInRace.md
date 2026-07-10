# Adding Inactive Competitors to a Race

By default, only **active** competitors can be added to a race — both when creating a new race
and when editing an existing one. This keeps the autocomplete box and quick-add buttons focused
on the boats that are currently racing.

Occasionally a scorekeeper needs to backfill an old race, or otherwise add a competitor who has
since been marked inactive. There's an undocumented flag for this rather than a permanent
checkbox in the UI, since it's a rare, edit-carefully operation.

## How to use it

Append `?includeInactive=true` to the race **Create** or **Edit** URL, e.g.:

```
https://sailscores.example.com/ClubInitials/Race/Edit/{raceId}?includeInactive=true
```

With the flag set:

- The competitor autocomplete box and the quick-add button row will include inactive
  competitors, each marked with `(inactive)` (autocomplete) or shown with a dashed, faded
  button style (quick-add row).
- Inactive competitors are still restricted to the race's selected **fleet** — the same
  fleet-membership rules (`AllBoatsInClub` / `SelectedClasses` / `SelectedBoats`) apply as for
  active competitors. This is not a way to browse the whole club's competitor list.
- Changing the fleet dropdown re-fetches the competitor list with the flag still applied, since
  it's read once from the URL when the page loads.

There is intentionally no button or checkbox to turn this on from within the page — it must be
added to the URL by hand (or via a bookmark), which is why this doc exists.

## Implementation notes

- `SailScores.Web/Scripts/raceEditor.ts`: `detectIncludeInactiveCompetitors()` reads the
  `includeInactive` query-string parameter once during `initialize()`; `getCompetitors()` passes
  it through to `GET /api/Competitors`.
- `SailScores.Web/Areas/Api/Controllers/CompetitorsController.cs` and
  `SailScores.Core/Services/CompetitorService.GetCompetitorsAsync(clubId, fleetId, includeInactive)`
  already supported this parameter (used elsewhere, e.g. the competitor list page's "show
  inactive" checkbox) — no server-side changes were needed to support this feature.
