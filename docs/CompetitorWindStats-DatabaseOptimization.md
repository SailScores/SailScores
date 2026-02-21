# Database Index Recommendations for Competitor Wind Stats

## Overview
To optimize the performance of the competitor wind statistics feature, consider adding the following database indexes.

## Recommended Indexes

### 1. Weather Table - Wind Speed Index
**Purpose**: Speed up queries filtering by WindSpeedMeterPerSecond

```sql
-- Create index on Weather.WindSpeedMeterPerSecond for faster filtering
CREATE NONCLUSTERED INDEX IX_Weather_WindSpeedMeterPerSecond
ON Weather (WindSpeedMeterPerSecond)
WHERE WindSpeedMeterPerSecond IS NOT NULL;
```

**Impact**: This filtered index will significantly speed up queries that filter races by wind conditions.

### 2. Composite Index on Scores Table
**Purpose**: Optimize the main query that joins Scores -> Race -> Weather

```sql
-- Create composite index for the wind stats query pattern
CREATE NONCLUSTERED INDEX IX_Scores_CompetitorId_Place_Include
ON Scores (CompetitorId, Place)
INCLUDE (RaceId)
WHERE Place IS NOT NULL;
```

**Impact**: This covering index will speed up filtering by competitor and place, with RaceId included for joining to races.

### 3. SeriesRaces Index (if not exists)
**Purpose**: Optimize season filtering through SeriesRaces

```sql
-- Create index for season-based filtering
CREATE NONCLUSTERED INDEX IX_SeriesRaces_RaceId_SeriesId
ON SeriesRaces (RaceId, SeriesId);
```

**Impact**: Speeds up the join when filtering by season.

## Performance Testing Recommendations

### Before Adding Indexes
1. Run the following query and note execution time:
```sql
-- Test query to measure performance
SET STATISTICS TIME ON;
SET STATISTICS IO ON;

SELECT 
    s.Place,
    w.WindSpeedMeterPerSecond,
    w.WindDirectionDegrees,
    (SELECT COUNT(*) FROM Scores sc WHERE sc.RaceId = r.Id AND sc.Place IS NOT NULL) as TotalStarters
FROM Scores s
INNER JOIN Races r ON s.RaceId = r.Id
INNER JOIN Weather w ON r.WeatherId = w.Id
WHERE s.CompetitorId = '00000000-0000-0000-0000-000000000000' -- Replace with actual GUID
    AND s.Place IS NOT NULL
    AND w.WindSpeedMeterPerSecond IS NOT NULL;

SET STATISTICS TIME OFF;
SET STATISTICS IO OFF;
```

### After Adding Indexes
Re-run the same query and compare:
- Execution time
- Logical reads
- Query plan

## Estimated Impact

### Without Indexes
- Query scans: Full table scans on Scores, Races, Weather
- Estimated reads: ~1000-5000 logical reads (depending on data size)
- Response time: 100-500ms

### With Indexes
- Query scans: Index seeks
- Estimated reads: ~10-50 logical reads
- Response time: 10-50ms (10x improvement)

## When to Add These Indexes

**Add immediately if:**
- You have more than 10,000 scores in the database
- You notice slow page load times for competitor details
- Database CPU usage spikes when viewing wind stats

**Can wait if:**
- Database has < 5,000 scores
- Performance is already acceptable
- You have very few races with weather data

## Index Maintenance

These indexes will:
- **Size**: Add approximately 1-5% to database size
- **Write Performance**: Minimal impact on INSERT/UPDATE operations (< 5% slower)
- **Read Performance**: 80-95% faster for wind stats queries

## Migration Script (Optional)

If you want to add these via EF Core migration:

```csharp
public partial class AddWindStatsIndexes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Weather wind speed index
        migrationBuilder.Sql(@"
            CREATE NONCLUSTERED INDEX IX_Weather_WindSpeedMeterPerSecond
            ON Weather (WindSpeedMeterPerSecond)
            WHERE WindSpeedMeterPerSecond IS NOT NULL;
        ");

        // Scores composite index
        migrationBuilder.Sql(@"
            CREATE NONCLUSTERED INDEX IX_Scores_CompetitorId_Place_Include
            ON Scores (CompetitorId, Place)
            INCLUDE (RaceId)
            WHERE Place IS NOT NULL;
        ");

        // SeriesRaces index (if not exists)
        migrationBuilder.Sql(@"
            IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SeriesRaces_RaceId_SeriesId')
            BEGIN
                CREATE NONCLUSTERED INDEX IX_SeriesRaces_RaceId_SeriesId
                ON SeriesRaces (RaceId, SeriesId);
            END
        ");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP INDEX IF EXISTS IX_Weather_WindSpeedMeterPerSecond ON Weather;");
        migrationBuilder.Sql("DROP INDEX IF EXISTS IX_Scores_CompetitorId_Place_Include ON Scores;");
        migrationBuilder.Sql("DROP INDEX IF EXISTS IX_SeriesRaces_RaceId_SeriesId ON SeriesRaces;");
    }
}
```

## Alternative: Query Store Recommendations

If you're using SQL Server 2016+ with Query Store enabled:
1. Enable Query Store on your database
2. Run the wind stats query several times
3. Check Query Store recommendations for automatic index suggestions

```sql
-- Enable Query Store
ALTER DATABASE YourDatabaseName SET QUERY_STORE = ON;
```

## Monitoring

After implementing these optimizations, monitor:
- Average query execution time
- Database CPU usage
- Page load time for competitor details with wind stats
- Cache hit ratio on the HTTP cache

Target metrics:
- Query execution: < 50ms
- Page load: < 200ms (including network)
- Cache hit ratio: > 95% after first day
