

Declare @ClubId UniqueIdentifier;
set @ClubId = (Select Id from Clubs where Initials = @ClubInitials)

-- Create temp table for series hierarchy to avoid recursive CTE overhead
CREATE TABLE #SeriesLinks (
	ParentSeriesId UniqueIdentifier NOT NULL,
	DescendentSeriesId UniqueIdentifier NOT NULL
)

-- Populate base case: each series links to itself
INSERT INTO #SeriesLinks (ParentSeriesId, DescendentSeriesId)
SELECT Id, Id
FROM Series s
WHERE s.ClubId = @ClubId

-- Populate recursive case: child series link to all parent ancestors
;WITH CTE_SeriesHierarchy AS (
	SELECT ParentSeriesId, DescendentSeriesId
	FROM #SeriesLinks
	UNION ALL
	SELECT s.ParentSeriesId, cte.DescendentSeriesId
	FROM dbo.SeriesToSeriesLink s
	INNER JOIN CTE_SeriesHierarchy cte ON s.ChildSeriesId = cte.ParentSeriesId
	WHERE s.ParentSeriesId IS NOT NULL
)
INSERT INTO #SeriesLinks (ParentSeriesId, DescendentSeriesId)
SELECT DISTINCT ParentSeriesId, DescendentSeriesId
FROM CTE_SeriesHierarchy
WHERE NOT EXISTS (
	SELECT 1 FROM #SeriesLinks sl2
	WHERE sl2.ParentSeriesId = CTE_SeriesHierarchy.ParentSeriesId
	AND sl2.DescendentSeriesId = CTE_SeriesHierarchy.DescendentSeriesId
)

-- Create index on temp table for join performance
CREATE CLUSTERED INDEX IX_SeriesLinks_Descendent ON #SeriesLinks(DescendentSeriesId)

-- Populate races with owning series into another temp table
CREATE TABLE #RacesWithOwningSeries (
	SeriesId UniqueIdentifier NOT NULL,
	SeriesName NVARCHAR(255),
	SeriesType INT,
	RaceId UniqueIdentifier NOT NULL,
	ParentSeriesId UniqueIdentifier NOT NULL,
	RaceDate DATE NOT NULL
)

INSERT INTO #RacesWithOwningSeries
SELECT
	s.ID as SeriesId,
	s.Name as SeriesName,
	s.Type as SeriesType,
	r.Id AS RaceId,
	sl.ParentSeriesId,
	r.Date as RaceDate
FROM Races r WITH (NOLOCK)
	INNER JOIN SeriesRace sr WITH (NOLOCK) ON r.Id = sr.RaceId
	INNER JOIN #SeriesLinks sl ON sr.SeriesId = sl.DescendentSeriesId
	INNER JOIN Series s WITH (NOLOCK) ON s.Id = sl.ParentSeriesId
WHERE r.ClubId = @ClubId
AND (@StartDate IS NULL OR r.Date >= @StartDate)
	AND (@EndDate IS NULL OR r.Date <= @EndDate)

-- Create indexes for subsequent joins
CREATE CLUSTERED INDEX IX_RacesWithOwningSeries_RaceId ON #RacesWithOwningSeries(RaceId)
CREATE NONCLUSTERED INDEX IX_RacesWithOwningSeries_RaceDate ON #RacesWithOwningSeries(RaceDate)
-- Pre-cache started score codes to avoid subquery in join
CREATE TABLE #ValidScoreCodes (
	ScoreName NVARCHAR(50) NOT NULL PRIMARY KEY CLUSTERED (ScoreName)
)

INSERT INTO #ValidScoreCodes
SELECT ScoreCodes.Name
FROM ScoreCodes WITH (NOLOCK)
inner join ScoringSystems with (nolock)
on ScoreCodes.ScoringSystemId = ScoringSystems.Id
WHERE ClubId = @ClubId AND ScoreCodes.[Started] = 1




    -----------------------------------------------------------------------
    -- Pre-aggregate per-race / per-class metrics to avoid repeated DISTINCTs
    -----------------------------------------------------------------------
    IF OBJECT_ID('tempdb..#RaceStats') IS NOT NULL DROP TABLE #RaceStats;

    CREATE TABLE #RaceStats (
        SeriesId UNIQUEIDENTIFIER NOT NULL,
        SeriesName NVARCHAR(255) NULL,
        SeriesType INT NULL,
        RaceId UNIQUEIDENTIFIER NOT NULL,
        RaceDate DATE NOT NULL,
        BoatClassName NVARCHAR(255) NULL,
        CompetitorsStarted INT NOT NULL,              -- count of score rows
        DistinctCompetitorsStarted INT NOT NULL       -- count distinct competitor ids
    );

    INSERT INTO #RaceStats (SeriesId, SeriesName, SeriesType, RaceId, RaceDate, BoatClassName, CompetitorsStarted, DistinctCompetitorsStarted)
    SELECT
        rws.SeriesId,
        rws.SeriesName,
        rws.SeriesType,
        rws.RaceId,
        rws.RaceDate,
        bc.Name AS BoatClassName,
        COUNT(*) AS CompetitorsStarted,
        COUNT(DISTINCT s.CompetitorId) AS DistinctCompetitorsStarted
    FROM #RacesWithOwningSeries rws
    JOIN dbo.Scores s WITH (NOLOCK) ON s.RaceId = rws.RaceId
    LEFT JOIN #ValidScoreCodes v ON s.Code = v.ScoreName
    -- allow NULL codes or only started score codes    
    LEFT JOIN dbo.Competitors comp WITH (NOLOCK) ON s.CompetitorId = comp.Id
    LEFT JOIN dbo.BoatClasses bc WITH (NOLOCK) ON bc.Id = comp.BoatClassId
    WHERE (s.Code IS NULL OR v.ScoreName IS NOT NULL)
    GROUP BY
        rws.SeriesId, rws.SeriesName, rws.SeriesType, rws.RaceId, rws.RaceDate, bc.Name;

    CREATE CLUSTERED INDEX IX_RaceStats_Series_Race ON #RaceStats(SeriesId, RaceId);
    CREATE NONCLUSTERED INDEX IX_RaceStats_RaceDate ON #RaceStats(RaceDate);

    -----------------------------------------------------------------------
    -- Final aggregation using pre-aggregated #RaceStats
    -----------------------------------------------------------------------
    SELECT
        se.Name AS SeasonName,
        MIN(se.[Start]) AS SeasonStart,
        MAX(rs.SeriesName) AS SeriesName,
        CASE
            WHEN MIN(rs.SeriesType) = 3 THEN 'Summary'
            WHEN MIN(rs.SeriesType) = 1 THEN 'Standard'
            WHEN MIN(rs.SeriesType) = 2 THEN 'Regatta'
            ELSE '' END AS SeriesType,
        COUNT(DISTINCT rs.BoatClassName) AS ClassCount,
        rs.BoatClassName AS ClassName,
        COUNT(DISTINCT rs.RaceId) AS RaceCount,
        SUM(rs.CompetitorsStarted) AS CompetitorsStarted,
        SUM(rs.DistinctCompetitorsStarted) AS DistinctCompetitorsStarted,
        CASE WHEN COUNT(DISTINCT rs.RaceId) = 0 THEN 0.0
             ELSE 1.0 * SUM(rs.CompetitorsStarted) / COUNT(DISTINCT rs.RaceId) END AS AverageCompetitorsPerRace,
        COUNT(DISTINCT rs.RaceDate) AS DistinctDaysRaced,
        MAX(rs.RaceDate) AS LastRace,
        MIN(rs.RaceDate) AS FirstRace
    FROM #RaceStats rs
    JOIN dbo.Seasons se WITH (NOLOCK)
        ON rs.RaceDate >= se.[Start] AND rs.RaceDate <= se.[End]
        AND se.ClubId = @ClubId
    WHERE (@StartDate IS NULL OR rs.RaceDate >= @StartDate)
      AND (@EndDate IS NULL OR rs.RaceDate <= @EndDate)
    GROUP BY
        se.Name,
        rs.SeriesId,
        ROLLUP (rs.BoatClassName)
    HAVING se.Name IS NOT NULL
      AND (COUNT(DISTINCT rs.BoatClassName) > 1 OR rs.BoatClassName IS NOT NULL)
    ORDER BY
        MIN(se.[Start]) DESC,
        MIN(rs.SeriesName),
        rs.BoatClassName;

    -----------------------------------------------------------------------
    -- Normal cleanup
    -----------------------------------------------------------------------
    DROP TABLE #RaceStats;
    DROP TABLE #ValidScoreCodes;
    DROP TABLE #RacesWithOwningSeries;
    DROP TABLE #SeriesLinks;
