# SailScores Fixture Tool

Turns club backups (`.json.gz` from Club Admin > Backup) into scoring test fixtures: one
file per series, holding only what's needed to score that series.

The tool has no project references and reads backups as raw JSON, so it can move to
another solution. New backup fields pass through unchanged.

## Commands

```
dotnet run --project SailScores.FixtureTool -- <command> ...
```

### `shared-systems`: export the shared base systems

```
shared-systems --connection "<connection string>" --out shared-systems.json
```

Club backups only hold the club's own scoring systems. Club systems inherit their
calculator type, settings and score codes from the shared base systems (`ClubId` null),
so extraction needs those too. This command exports them, along with the shared handicap
systems, from any database that has them (a local copy of production is fine). It reads
every column, so it works against older schemas.

### `list`: inventory a backup

```
list <backup.json.gz> --shared shared-systems.json [--out series.csv]
```

Writes one CSV row per series: races, child series, competitors, fleets, effective
scoring system, calculator type, discard method and pattern, participation %, score
codes used, tied places, handicap system and whether stored results exist. Use it to
pick a small set of series that covers every feature in use.

### `extract`: write fixtures

```
extract <backup.json.gz> --out <dir> --shared shared-systems.json
        (--series <id | urlName | season/urlName> ... | --all) [--gzip]
```

Each fixture is still a valid club backup, so `BackupService.RestoreBackupAsync` can load
it. It contains:

- the series and all of its descendant series (for summary series)
- their races and scores
- the competitors in those scores, plus every member of the fleets involved (calculators
  can score fleet members who never raced)
- the fleets, boat classes and seasons involved
- every scoring system the series could fall back to (regatta, series, club default),
  with all ancestors, including the shared base systems
- the club's handicap systems and the competitors' handicaps
- the current stored results for each series (`historicalResults`), which hold the
  expected output as the site last calculated it
- a `fixtureInfo` object recording the source series and any warnings

### `anonymize`: rename competitors in existing fixtures

```
anonymize <fixture.json[.gz]> ...
```

Replaces competitor names as `extract` does (see below), for fixtures written before it
did. Rewrites each file in place; running it again changes nothing.

## What gets removed

- **User names (email addresses)**: every `createdBy`, `updatedBy` and `changedBy`,
  including inside the stored results JSON. Anything else that looks like an email
  address is replaced with `redacted@example.com`, and the tool reports how many.
- **Data scoring never reads**: announcements, documents, files, forwarders, competitor
  change history, club sequences, chart data, weather, and free text (descriptions,
  notes, tracking URLs, club URL).

- **Competitor names**: replaced by `Comp` and the initials of each word, in the order
  written ("Smith, Jane" becomes `CompSJ`; repeats are numbered, `CompSJ2`), including in
  the stored results. URL names made from a name are replaced too. Names are built from
  the whole backup, so a competitor has the same name in every fixture from it.
- **Race descriptions in the stored results**: free text, which can name race officers.

Sail numbers, boat names, home clubs and dates are kept.

## Older backups

Backups taken before the backup fix of October 2026 is deployed leave out score code groups on club systems and season
default scoring systems. Fixtures from them restore without those, so a series that
relies on either won't score as it does on the site. Take a fresh backup.
