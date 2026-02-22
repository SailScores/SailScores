# SailScores Club Backup Schema

This document describes the JSON schema of SailScores club backup files, intended for
developers who need to restore data or for users who want to export data into other
systems such as spreadsheets.

## File Format

Backup files are GZip-compressed JSON with the extension `.json.gz`. The file can also
be uncompressed JSON (the reader auto-detects via GZip magic bytes `1f 8b`).

All property names use **camelCase** in the JSON output. Enum values are serialized as
**strings** (not integers) for forward compatibility.

Null-valued properties are omitted from the output.

---

## Top-Level Structure

```jsonc
{
  "metadata": { ... },           // Required – see Metadata below

  // Club settings
  "name": "string",
  "initials": "string",
  "description": "string | null",
  "homePageDescription": "string | null",
  "isHidden": false,
  "showClubInResults": true,
  "showCalendarInNav": true,
  "url": "string | null",
  "locale": "string | null",
  "defaultRaceDateOffset": 0,
  "statisticsDescription": "string | null",
  "weatherSettings": { ... },    // See WeatherSettings
  "logoFileId": "guid | null",
  "defaultScoringSystemName": "string | null",

  // Entity collections
  "boatClasses": [ ... ],
  "seasons": [ ... ],
  "fleets": [ ... ],
  "competitors": [ ... ],
  "scoringSystems": [ ... ],
  "series": [ ... ],
  "races": [ ... ],
  "regattas": [ ... ],
  "announcements": [ ... ],
  "documents": [ ... ],
  "clubSequences": [ ... ],
  "competitorForwarders": [ ... ],
  "regattaForwarders": [ ... ],
  "seriesForwarders": [ ... ],
  "files": [ ... ],
  "seriesChartResults": [ ... ],
  "historicalResults": [ ... ]
}
```

---

## Metadata

| Property | Type | Description |
|---|---|---|
| `schema` | string | Always `"sailscores-club-backup"`. Used to identify the file type. |
| `version` | int | Schema version. Current version: **1**. |
| `createdDateUtc` | datetime | UTC timestamp when the backup was created. |
| `sourceClubId` | guid | The club ID at time of export. |
| `sourceClubInitials` | string | Club initials at time of export. |
| `sourceClubName` | string | Club name at time of export. |
| `createdBy` | string | Username of the person who created the backup. |
| `boatClassCount` | int? | Number of boat classes in the backup. |
| `competitorCount` | int? | Number of competitors in the backup. |
| `fleetCount` | int? | Number of fleets in the backup. |
| `raceCount` | int? | Number of races in the backup. |
| `scoreCount` | int? | Total number of individual scores across all races. |
| `seasonCount` | int? | Number of seasons in the backup. |
| `seriesCount` | int? | Number of series in the backup. |
| `regattaCount` | int? | Number of regattas in the backup. |
| `scoringSystemCount` | int? | Number of club-owned scoring systems in the backup. |

Entity counts are populated during export and can be used for quick validation before
a full restore.

---

## Entity Definitions

### BoatClass

| Property | Type | Description |
|---|---|---|
| `id` | guid | Unique identifier. |
| `name` | string | Class name (e.g. "Laser", "Sunfish"). |
| `description` | string? | Optional description. |

### Season

| Property | Type | Description |
|---|---|---|
| `id` | guid | Unique identifier. |
| `name` | string | Season name (e.g. "2024"). |
| `urlName` | string | URL-safe name. |
| `start` | datetime | Season start date. |
| `end` | datetime | Season end date. |

### Fleet

| Property | Type | Description |
|---|---|---|
| `id` | guid | Unique identifier. |
| `name` | string | Fleet name. |
| `shortName` | string? | Abbreviated name. |
| `nickName` | string? | Informal name. |
| `description` | string? | Description. |
| `isActive` | bool? | Whether the fleet is active. |
| `fleetType` | string | `"SelectedBoats"`, `"SelectedClasses"` or `"AllBoatsInClub"`. |
| `boatClassIds` | guid[] | References to boat classes in this fleet. |

### Competitor

| Property | Type | Description |
|---|---|---|
| `id` | guid | Unique identifier. |
| `name` | string | Competitor name (typically skipper name). |
| `sailNumber` | string? | Primary sail number. |
| `alternativeSailNumber` | string? | Temporary or regatta-specific sail number. |
| `boatName` | string? | Name of the boat. |
| `homeClubName` | string? | For regatta visitors, their home club. |
| `notes` | string? | Free-text notes. |
| `isActive` | bool? | Whether the competitor is active. |
| `boatClassId` | guid | Reference to the competitor's boat class. |
| `urlName` | string? | URL-safe name. |
| `urlId` | string? | URL identifier. |
| `created` | datetime? | Creation timestamp. |
| `fleetIds` | guid[] | Fleets this competitor belongs to. |

### ScoringSystem

| Property | Type | Description |
|---|---|---|
| `id` | guid | Unique identifier. |
| `name` | string | System name (e.g. "Appendix A For Series"). |
| `discardPattern` | string? | Comma-separated discard counts (e.g. `"0,1,1,1"`). |
| `participationPercent` | decimal? | Minimum participation percentage for ranking. |
| `parentSystemId` | guid? | Reference to a parent scoring system. May point to a site-wide system not included in this backup. |
| `isSiteDefault` | bool? | Whether this is a site-level default system. |
| `scoreCodes` | ScoreCode[] | Custom score codes for this system. |

### ScoreCode

| Property | Type | Description |
|---|---|---|
| `id` | guid | Unique identifier. |
| `name` | string | Code abbreviation (e.g. "DNS", "DNF", "OCS"). |
| `description` | string? | Human-readable description. |
| `formula` | string? | Scoring formula (e.g. `"RacesScored + 1"`). |
| `formulaValue` | int? | Fixed value used in formula. |
| `scoreLike` | string? | Score like another code. |
| `discardable` | bool? | Whether this code can be discarded. |
| `cameToStart` | bool? | Whether this counts as coming to the start. |
| `started` | bool? | Whether the competitor started. |
| `finished` | bool? | Whether the competitor finished. |
| `preserveResult` | bool? | Keep the original numeric result. |
| `adjustOtherScores` | bool? | Whether this adjusts other competitors' scores. |
| `countAsParticipation` | bool? | Whether this counts toward participation percentage. |

### Series

| Property | Type | Description |
|---|---|---|
| `id` | guid | Unique identifier. |
| `name` | string | Series name. |
| `urlName` | string? | URL-safe name. |
| `description` | string? | Description. |
| `type` | string? | `"Standard"` or `"Summary"`. |
| `isImportantSeries` | bool? | Highlighted on the club page. |
| `resultsLocked` | bool? | Historical series with frozen results. |
| `updatedDate` | datetime? | Last update timestamp. |
| `updatedBy` | string? | Last updating user. |
| `scoringSystemId` | guid? | Reference to a scoring system. |
| `trendOption` | string? | Trend display option. |
| `fleetId` | guid? | Reference to a fleet. |
| `preferAlternativeSailNumbers` | bool? | Use alternative sail numbers. |
| `excludeFromCompetitorStats` | bool? | Exclude from competitor statistics. |
| `hideDncDiscards` | bool? | Hide DNC discards in display. |
| `childrenSeriesAsSingleRace` | bool? | Treat child series as single race in summary. |
| `raceCount` | int? | Number of races in the series. |
| `dateRestricted` | bool? | Whether dates are restricted. |
| `enforcedStartDate` | date? | Start of allowed date range. |
| `enforcedEndDate` | date? | End of allowed date range. |
| `startDate` | date? | Series start date. |
| `endDate` | date? | Series end date. |
| `seasonId` | guid? | Reference to a season. |
| `childrenSeriesIds` | guid[] | Child series for summary series. |
| `parentSeriesIds` | guid[] | Parent series references. |

### Race

| Property | Type | Description |
|---|---|---|
| `id` | guid | Unique identifier. |
| `name` | string? | Race name (e.g. "Race 1"). |
| `date` | datetime? | Race date. |
| `state` | string? | `"Raced"`, `"Scheduled"`, or `"Abandoned"`. |
| `order` | int | Display/sorting order. |
| `description` | string? | Description. |
| `trackingUrl` | string? | URL for race tracking. |
| `updatedDate` | datetime? | Last update timestamp. |
| `updatedBy` | string? | Last updating user. |
| `startTime` | datetime? | Race start time. |
| `trackTimes` | bool | Whether finish times are tracked. |
| `fleetId` | guid? | Reference to a fleet. |
| `weather` | Weather? | Weather conditions during the race. |
| `scores` | Score[] | Individual competitor results. |
| `seriesIds` | guid[] | Series this race belongs to. |

### Score

A single competitor's result in a single race.

| Property | Type | Description |
|---|---|---|
| `id` | guid | Unique identifier. |
| `competitorId` | guid | Reference to the competitor. |
| `raceId` | guid | Reference to the race. |
| `place` | int? | Finishing position (null if a code is used). |
| `code` | string? | Score code (e.g. "DNS", "DNF"). Null for numeric place. |
| `codePoints` | decimal? | Points assigned for this code. |
| `finishTime` | datetime? | Absolute finish time. |
| `elapsedTime` | timespan? | Elapsed race time. |

**Spreadsheet note:** To flatten scores for a spreadsheet, join each score with its
parent race (by `raceId`) and with competitors (by `competitorId`). The key columns
for a results spreadsheet would be: race name, race date, competitor name, sail number,
place, and code.

### Weather

| Property | Type | Description |
|---|---|---|
| `id` | guid | Unique identifier. |
| `description` | string? | Weather description text. |
| `icon` | string? | Weather icon identifier. |
| `temperatureString` | string? | Display-formatted temperature. |
| `temperatureDegreesKelvin` | decimal? | Temperature in Kelvin. |
| `windSpeedString` | string? | Display-formatted wind speed. |
| `windSpeedMeterPerSecond` | decimal? | Wind speed in m/s. |
| `windDirectionString` | string? | Display-formatted wind direction. |
| `windDirectionDegrees` | decimal? | Wind direction in degrees. |
| `windGustString` | string? | Display-formatted gust speed. |
| `windGustMeterPerSecond` | decimal? | Gust speed in m/s. |
| `humidity` | decimal? | Humidity percentage. |
| `cloudCoverPercent` | decimal? | Cloud cover percentage. |
| `createdDate` | datetime? | When the weather data was recorded. |

### WeatherSettings

Club-level weather configuration.

| Property | Type | Description |
|---|---|---|
| `latitude` | decimal? | Club location latitude. |
| `longitude` | decimal? | Club location longitude. |
| `temperatureUnits` | string? | Temperature unit preference. |
| `windSpeedUnits` | string? | Wind speed unit preference. |

### Regatta

| Property | Type | Description |
|---|---|---|
| `id` | guid | Unique identifier. |
| `name` | string | Regatta name. |
| `urlName` | string? | URL-safe name. |
| `description` | string? | Description. |
| `url` | string? | External URL for the regatta. |
| `startDate` | datetime? | Event start date. |
| `endDate` | datetime? | Event end date. |
| `updatedDate` | datetime? | Last update timestamp. |
| `scoringSystemId` | guid? | Reference to a scoring system. |
| `preferAlternateSailNumbers` | bool? | Use alternative sail numbers. |
| `hideFromFrontPage` | bool? | Keep off the public front page. |
| `seasonId` | guid? | Reference to a season. |
| `seriesIds` | guid[] | Series belonging to this regatta. |
| `fleetIds` | guid[] | Fleets participating in this regatta. |

### Announcement

| Property | Type | Description |
|---|---|---|
| `id` | guid | Unique identifier. |
| `regattaId` | guid? | Associated regatta (null for club announcements). |
| `content` | string | Announcement text/HTML. |
| `createdDate` | datetime | UTC creation time. |
| `createdLocalDate` | datetime | Local creation time. |
| `createdBy` | string? | Creating user. |
| `updatedDate` | datetime? | UTC update time. |
| `updatedLocalDate` | datetime? | Local update time. |
| `updatedBy` | string? | Last updating user. |
| `archiveAfter` | datetime? | Auto-archive date. |

### Document

| Property | Type | Description |
|---|---|---|
| `id` | guid | Unique identifier. |
| `regattaId` | guid? | Associated regatta. |
| `name` | string | Document name. |
| `contentType` | string | MIME type (e.g. `"application/pdf"`). |
| `fileContents` | base64 | Document binary content, base64-encoded. |
| `createdDate` | datetime | UTC creation time. |
| `createdLocalDate` | datetime | Local creation time. |
| `createdBy` | string? | Creating user. |

### ClubSequence

Auto-increment sequences used by the club.

| Property | Type | Description |
|---|---|---|
| `id` | guid | Unique identifier. |
| `nextValue` | int | Next value in the sequence. |
| `sequenceType` | string | Sequence category (e.g. `"Competitor"`). |
| `sequencePrefix` | string? | Prefix for generated values. |
| `sequenceSuffix` | string? | Suffix for generated values. |

### File

Binary files (currently used for club logo).

| Property | Type | Description |
|---|---|---|
| `id` | guid | Unique identifier. |
| `fileContents` | base64 | File binary content, base64-encoded. |
| `created` | datetime | Creation timestamp. |
| `importedTime` | datetime? | Import timestamp. |

### CompetitorForwarder

URL redirect for renamed competitors.

| Property | Type | Description |
|---|---|---|
| `id` | guid | Unique identifier. |
| `oldClubInitials` | string | Previous club initials. |
| `oldCompetitorUrl` | string | Previous URL path. |
| `competitorId` | guid | Target competitor. |
| `created` | datetime | Creation timestamp. |

### RegattaForwarder

URL redirect for renamed regattas.

| Property | Type | Description |
|---|---|---|
| `id` | guid | Unique identifier. |
| `oldClubInitials` | string | Previous club initials. |
| `oldSeasonUrlName` | string | Previous season URL name. |
| `oldRegattaUrlName` | string | Previous regatta URL name. |
| `regattaId` | guid | Target regatta. |
| `created` | datetime | Creation timestamp. |

### SeriesForwarder

URL redirect for renamed series.

| Property | Type | Description |
|---|---|---|
| `id` | guid | Unique identifier. |
| `oldClubInitials` | string | Previous club initials. |
| `oldSeasonUrlName` | string | Previous season URL name. |
| `oldSeriesUrlName` | string | Previous series URL name. |
| `seriesId` | guid | Target series. |
| `created` | datetime | Creation timestamp. |

### SeriesChartResults

Cached chart data for a series.

| Property | Type | Description |
|---|---|---|
| `id` | guid | Unique identifier. |
| `seriesId` | guid | Reference to the series. |
| `isCurrent` | bool | Whether this is the current cached version. |
| `results` | string | Serialized chart data (JSON string). |
| `created` | datetime | Cache creation timestamp. |

### HistoricalResults

Cached results to speed up series display.

| Property | Type | Description |
|---|---|---|
| `id` | guid | Unique identifier. |
| `seriesId` | guid | Reference to the series. |
| `isCurrent` | bool | Whether this is the current cached version. |
| `results` | string | Serialized results data (JSON string). |
| `created` | datetime | Cache creation timestamp. |

---

## Relationships

Entities reference each other via GUID IDs. When restoring, all GUIDs are remapped to
new values to avoid conflicts. The key relationships are:

- **Competitor** → `boatClassId` → **BoatClass**
- **Competitor** → `fleetIds[]` → **Fleet** (many-to-many)
- **Fleet** → `boatClassIds[]` → **BoatClass** (many-to-many)
- **Series** → `seasonId` → **Season**
- **Series** → `scoringSystemId` → **ScoringSystem**
- **Series** → `fleetId` → **Fleet**
- **Series** → `childrenSeriesIds[]` / `parentSeriesIds[]` → **Series** (self-referencing)
- **Race** → `fleetId` → **Fleet**
- **Race** → `seriesIds[]` → **Series** (many-to-many)
- **Score** → `competitorId` → **Competitor**
- **Score** → `raceId` → **Race**
- **Regatta** → `seasonId` → **Season**
- **Regatta** → `scoringSystemId` → **ScoringSystem**
- **Regatta** → `seriesIds[]` → **Series** (many-to-many)
- **Regatta** → `fleetIds[]` → **Fleet** (many-to-many)
- **ScoringSystem** → `parentSystemId` → **ScoringSystem** (may reference site-wide system)
- **Announcement** → `regattaId` → **Regatta**
- **Document** → `regattaId` → **Regatta**

---

## Importing Into a Spreadsheet

The most useful data for spreadsheet analysis is the race results. To build a flat
results table:

1. Parse the JSON (decompress first if `.json.gz`).
2. For each race in `races`, iterate its `scores` array.
3. Look up the competitor by `competitorId` in the `competitors` array.
4. Look up the series names by joining `race.seriesIds` with the `series` array.

**Suggested columns:**

| Race Name | Race Date | Series | Competitor | Sail Number | Place | Code | Points |
|---|---|---|---|---|---|---|---|
| Race 1 | 2024-06-06 | Summer Series | John Sailor | 12345 | 1 | | |
| Race 1 | 2024-06-06 | Summer Series | Jane Racer | 67890 | 2 | | |
| Race 2 | 2024-06-13 | Summer Series | John Sailor | 12345 | | DNS | 3 |

---

## Versioning

The `metadata.version` field tracks breaking schema changes. The current version is
**1**. When restoring, the application rejects backups with a version higher than what
it supports. Older versions will be supported through migration logic in future
releases.

The `metadata.schema` field (`"sailscores-club-backup"`) identifies the file format
and distinguishes it from other JSON files.

---

## Notes

- **Scorekeeper information** (user accounts, permissions) is excluded from backups.
  Only `createdBy` / `updatedBy` display names are preserved.
- **Site-wide scoring systems** (those not owned by the club) are not included. The
  `parentSystemId` on a club scoring system may reference a site-wide system by its
  original GUID; this works when restoring to the same server.
- **Binary data** (`fileContents` in documents and files) is base64-encoded in the JSON
  output.
- **Entity counts** in metadata are optional but recommended for validation. They are
  populated during export and checked during import.
