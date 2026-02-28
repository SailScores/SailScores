# Database Schema Quick Reference

A fast lookup guide for the SailScores database schema.

## Table Quick Reference

| Table | Purpose | Key Fields |
|-------|---------|-----------|
| **Club** | Top-level organization | Id, Initials, Name |
| **Season** | Time period containing series | Id, ClubId, Start, End |
| **Series** | Collection of races scored together | Id, ClubId, SeasonId, Type, FleetId (regatta only) |
| **Race** | Individual race event | Id, ClubId, Date, FleetId, State |
| **SeriesRace** | Links races to series | SeriesId, RaceId (junction) |
| **Fleet** | Group of competitors | Id, ClubId, Name, IsActive |
| **ScoringSystem** | Scoring rules | Id, ClubId, Name, IsDefault |
| **Score** | Competitor's score in a race | Id, RaceId, CompetitorId, ScoreValue |
| **SeriesToSeriesLink** | Summary → Standard series | ParentSeriesId, ChildSeriesId |
| **CompetitorFleet** | Competitor → Fleet mapping | CompetitorId, FleetId |

## Key Relationships at a Glance

```
Club
 ├── Season
 │    └── Series
 │         ├── SeriesRace
 │         │    └── Race ─→ Fleet
 │         └── SeriesToSeriesLink (parent/child)
 │
 ├── Fleet
 │    ├── Race
 │    ├── CompetitorFleet
 │    └── FleetBoatClass
 │
 └── ScoringSystem
      └── Series (optional)
```

## Common Lookups

### "I need to find all races in a series"
**Query**: `SELECT * FROM SeriesRace WHERE SeriesId = @id`  
**Then**: Join to Races table on RaceId  
**Key**: Use SeriesRace junction table, not direct Series.Races navigation

### "I need to find which fleets are in a series"
**Query**:
```sql
SELECT DISTINCT f.Name
FROM SeriesRace sr
INNER JOIN Race r ON sr.RaceId = r.Id
INNER JOIN Fleet f ON r.FleetId = f.Id
WHERE sr.SeriesId = @SeriesId
```
**Key**: A series can have races from multiple fleets

### "I need to find child series of a summary series"
**Query**:
```sql
SELECT * FROM SeriesToSeriesLink 
WHERE ParentSeriesId = @SummarySeriesId
```
**Key**: Only Summary type series have children

### "I need to find which series use a scoring system"
**Query**: `SELECT * FROM Series WHERE ScoringSystemId = @id`  
**Key**: NULL ScoringSystemId means use club default

### "I need competitor results in a fleet for a race"
**Query**:
```sql
SELECT s.* FROM Score s
WHERE s.RaceId = @RaceId
  AND EXISTS (
    SELECT 1 FROM CompetitorFleet cf
    WHERE cf.CompetitorId = s.CompetitorId
      AND cf.FleetId = @FleetId
  )
```

## Enums at a Glance

### Series Type (Type field)
- `0` = Standard (regular races)
- `1` = Summary (aggregates other series)
- `2` = Regatta (special regatta rules)

### Race State (State field)
- `Pending` = Not yet completed
- `Completed` = Results finalized
- `Abandoned`, `NoWind`, `PostponedBad`, `PostponedEquipment` = Not scored

### Fleet Type (FleetType field)
- `0` = Standard
- `1` = Cruising
- `2` = Racing
- `3` = OneDesign

## Critical Constraints

| Rule | Impact | Example |
|------|--------|---------|
| Each Race has exactly one Fleet | Can't assign race to multiple fleets | Must choose which fleet each race belongs to |
| Series can have multiple Fleet types | Series can span multiple fleets | Valid: Series with races from Fleet A and Fleet B |
| SeriesRace is the junction | Must use it to query series races | Don't query Race directly from Series |
| Season dates bound series dates | Series dates must be in season | Can't create race outside season dates |
| Club is ultimate parent | All entities belong to a club | Must always filter by ClubId |

## What Are Multi-Fleet Series?

A **multi-fleet series** is a series containing races assigned to different fleets.

**Example**:
- Series: "Summer Championship 2024"
  - Race 1: Fleet A
  - Race 2: Fleet B
  - Race 3: Fleet A

**How to detect**:
```sql
SELECT sr.SeriesId, COUNT(DISTINCT r.FleetId) AS FleetCount
FROM SeriesRace sr
INNER JOIN Race r ON sr.RaceId = r.Id
GROUP BY sr.SeriesId
HAVING COUNT(DISTINCT r.FleetId) > 1
```

**Why it matters**: When implementing a "Default Fleet" feature, multi-fleet series need special handling.

## Performance Tips

1. **Always filter by ClubId early** - Reduces dataset before joins
2. **Use SeriesRace for series queries** - More efficient than navigating relationships
3. **Index on (ClubId, SeasonId)** - Common filter combination
4. **Denormalize complex queries** - Consider views for frequently used patterns
5. **Use IsActive flags** - Reduces results for "current" queries

## Validation Queries

### Check table structure
```sql
SELECT TABLE_NAME, COUNT(*) AS ColumnCount
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = 'dbo'
GROUP BY TABLE_NAME
ORDER BY TABLE_NAME
```

### Find all relationships
```sql
SELECT TABLE_NAME, COLUMN_NAME, REFERENCED_TABLE_NAME
FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE
WHERE TABLE_SCHEMA = 'dbo'
  AND REFERENCED_TABLE_NAME IS NOT NULL
ORDER BY TABLE_NAME
```

### Find NULL usage
```sql
SELECT TABLE_NAME, COLUMN_NAME, IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = 'dbo'
  AND IS_NULLABLE = 'YES'
ORDER BY TABLE_NAME, COLUMN_NAME
```

## Resources

- **Full Schema**: `docs/DatabaseSchema.md`
- **Entity Models**: `SailScores.Database/Entities/`
- **Context**: `SailScores.Database/SailScoresContext.cs`
- **Migrations**: `SailScores.Database/Migrations/`
- **Query Examples**: See Common Query Patterns in `docs/DatabaseSchema.md`
