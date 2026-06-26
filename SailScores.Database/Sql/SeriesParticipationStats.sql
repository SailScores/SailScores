


DECLARE @ClubId AS UNIQUEIDENTIFIER;

SET @ClubId = (SELECT Id
               FROM   Clubs
               WHERE  Initials = @ClubInitials); -- Create temp table for series hierarchy to avoid recursive CTE overhead

CREATE TABLE #SeriesLinks (
    ParentSeriesId     UNIQUEIDENTIFIER NOT NULL,
    DescendentSeriesId UNIQUEIDENTIFIER NOT NULL
); -- Populate base case: each series links to itself

INSERT INTO #SeriesLinks (ParentSeriesId, DescendentSeriesId)
SELECT Id,
       Id
FROM   Series AS s
WHERE  s.ClubId = @ClubId -- Populate recursive case: child series link to all parent ancestors;
;

WITH CTE_SeriesHierarchy
AS   (SELECT ParentSeriesId,
             DescendentSeriesId
      FROM   #SeriesLinks
      UNION ALL
      SELECT s.ParentSeriesId,
             cte.DescendentSeriesId
      FROM   dbo.SeriesToSeriesLink AS s
             INNER JOIN
             CTE_SeriesHierarchy AS cte
             ON s.ChildSeriesId = cte.ParentSeriesId
      WHERE  s.ParentSeriesId IS NOT NULL)
INSERT INTO #SeriesLinks (ParentSeriesId, DescendentSeriesId)
SELECT DISTINCT ParentSeriesId,
                DescendentSeriesId
FROM   CTE_SeriesHierarchy
WHERE  NOT EXISTS (SELECT 1
                   FROM   #SeriesLinks AS sl2
                   WHERE  sl2.ParentSeriesId = CTE_SeriesHierarchy.ParentSeriesId
                          AND sl2.DescendentSeriesId = CTE_SeriesHierarchy.DescendentSeriesId); -- Create index on temp table for join performance

CREATE CLUSTERED INDEX IX_SeriesLinks_Descendent
    ON #SeriesLinks(DescendentSeriesId); -- Populate races with owning series into another temp table

CREATE TABLE #RacesWithOwningSeries (
    SeriesId       UNIQUEIDENTIFIER NOT NULL,
    SeriesName     NVARCHAR (255)  ,
    SeriesType     INT             ,
    RaceId         UNIQUEIDENTIFIER NOT NULL,
    ParentSeriesId UNIQUEIDENTIFIER NOT NULL,
    RaceDate       DATE             NOT NULL
);

INSERT INTO #RacesWithOwningSeries
SELECT s.ID AS SeriesId,
       s.Name AS SeriesName,
       s.Type AS SeriesType,
       r.Id AS RaceId,
       sl.ParentSeriesId,
       r.Date AS RaceDate
FROM   Races AS r WITH (NOLOCK)
       INNER JOIN
       SeriesRace AS sr WITH (NOLOCK)
       ON r.Id = sr.RaceId
       INNER JOIN
       #SeriesLinks AS sl
       ON sr.SeriesId = sl.DescendentSeriesId
       INNER JOIN
       Series AS s WITH (NOLOCK)
       ON s.Id = sl.ParentSeriesId
WHERE  r.ClubId = @ClubId
       AND (@StartDate IS NULL
            OR r.Date >= @StartDate)
       AND (@EndDate IS NULL
            OR r.Date <= @EndDate); -- Create indexes for subsequent joins

CREATE CLUSTERED INDEX IX_RacesWithOwningSeries_RaceId
    ON #RacesWithOwningSeries(RaceId);

CREATE NONCLUSTERED INDEX IX_RacesWithOwningSeries_RaceDate
    ON #RacesWithOwningSeries(RaceDate); -- Pre-cache started score codes to avoid subquery in join

CREATE TABLE #ValidScoreCodes (
    ScoreName NVARCHAR (50) NOT NULL PRIMARY KEY CLUSTERED (ScoreName)
);

INSERT INTO #ValidScoreCodes
SELECT ScoreCodes.Name
FROM   ScoreCodes WITH (NOLOCK)
       INNER JOIN
       ScoringSystems WITH (NOLOCK)
       ON ScoreCodes.ScoringSystemId = ScoringSystems.Id
WHERE  ClubId = @ClubId
       AND ScoreCodes.[Started] = 1;

------------------------------- -- Pre-aggregate per-race / per-class metrics to avoid repeated DISTINCTs --------------------------------

IF OBJECT_ID('tempdb..#RaceStats') IS NOT NULL
    DROP TABLE #RaceStats;

CREATE TABLE #RaceStats (
    SeriesId                   UNIQUEIDENTIFIER NOT NULL,
    SeriesName                 NVARCHAR (255)   NULL,
    SeriesType                 INT              NULL,
    RaceId                     UNIQUEIDENTIFIER NOT NULL,
    RaceDate                   DATE             NOT NULL,
    BoatClassName              NVARCHAR (255)   NULL,
    CompetitorsStarted         INT              NOT NULL,
    -- count of score rows
    DistinctCompetitorsStarted INT              NOT NULL -- count distinct competitor ids
);

INSERT INTO #RaceStats (SeriesId, SeriesName, SeriesType, RaceId, RaceDate, BoatClassName, CompetitorsStarted, DistinctCompetitorsStarted)
SELECT   rws.SeriesId,
         rws.SeriesName,
         rws.SeriesType,
         rws.RaceId,
         rws.RaceDate,
         bc.Name AS BoatClassName,
         COUNT(*) AS CompetitorsStarted,
         COUNT(DISTINCT s.CompetitorId) AS DistinctCompetitorsStarted
FROM     #RacesWithOwningSeries AS rws
         INNER JOIN
         dbo.Scores AS s WITH (NOLOCK)
         ON s.RaceId = rws.RaceId
         LEFT OUTER JOIN
         #ValidScoreCodes AS v
         ON s.Code = v.ScoreName -- allow NULL codes or only started score codes    
         LEFT OUTER JOIN
         dbo.Competitors AS comp WITH (NOLOCK)
         ON s.CompetitorId = comp.Id
         LEFT OUTER JOIN
         dbo.BoatClasses AS bc WITH (NOLOCK)
         ON bc.Id = comp.BoatClassId
WHERE    (s.Code IS NULL
          OR v.ScoreName IS NOT NULL)
GROUP BY rws.SeriesId, rws.SeriesName, rws.SeriesType, rws.RaceId, rws.RaceDate, bc.Name;

CREATE CLUSTERED INDEX IX_RaceStats_Series_Race
    ON #RaceStats(SeriesId, RaceId);

CREATE NONCLUSTERED INDEX IX_RaceStats_RaceDate
    ON #RaceStats(RaceDate);

----------------------------- -- Build distinct competitor rows per race/class (one row per competitor) ------------------------------------

IF OBJECT_ID('tempdb..#RaceCompetitors') IS NOT NULL
    DROP TABLE #RaceCompetitors;

CREATE TABLE #RaceCompetitors (
    SeriesId      UNIQUEIDENTIFIER NOT NULL,
    SeriesName    NVARCHAR (255)   NULL,
    SeriesType    INT              NULL,
    RaceId        UNIQUEIDENTIFIER NOT NULL,
    RaceDate      DATE             NOT NULL,
    BoatClassName NVARCHAR (255)   NULL,
    CompetitorId  UNIQUEIDENTIFIER NOT NULL
);

INSERT INTO #RaceCompetitors (SeriesId, SeriesName, SeriesType, RaceId, RaceDate, BoatClassName, CompetitorId)
SELECT DISTINCT rws.SeriesId,
                rws.SeriesName,
                rws.SeriesType,
                rws.RaceId,
                rws.RaceDate,
                bc.Name AS BoatClassName,
                s.CompetitorId
FROM   #RacesWithOwningSeries AS rws
       INNER JOIN
       dbo.Scores AS s WITH (NOLOCK)
       ON s.RaceId = rws.RaceId
       LEFT OUTER JOIN
       #ValidScoreCodes AS v
       ON s.Code = v.ScoreName
       LEFT OUTER JOIN
       dbo.Competitors AS comp WITH (NOLOCK)
       ON s.CompetitorId = comp.Id
       LEFT OUTER JOIN
       dbo.BoatClasses AS bc WITH (NOLOCK)
       ON bc.Id = comp.BoatClassId
WHERE  (s.Code IS NULL
        OR v.ScoreName IS NOT NULL)
       AND s.CompetitorId IS NOT NULL;

CREATE NONCLUSTERED INDEX IX_RaceCompetitors_RaceDate
    ON #RaceCompetitors(RaceDate);

CREATE NONCLUSTERED INDEX IX_RaceCompetitors_SeriesId_Class
    ON #RaceCompetitors(SeriesId, BoatClassName);
------------------------------------- -- Precompute distinct competitor counts per season+series+class -- and per season+series (series-level) so GROUP ROLLUP works. ---------------------------

IF OBJECT_ID('tempdb..#SeasonDistinctClassCounts') IS NOT NULL
    DROP TABLE #SeasonDistinctClassCounts;

CREATE TABLE #SeasonDistinctClassCounts (
    SeasonName                 NVARCHAR (255)   NOT NULL,
    SeriesId                   UNIQUEIDENTIFIER NOT NULL,
    BoatClassName              NVARCHAR (255)   NULL,
    DistinctCompetitorsStarted INT           NOT NULL
);

INSERT INTO #SeasonDistinctClassCounts (SeasonName, SeriesId, BoatClassName, DistinctCompetitorsStarted)
SELECT   se.Name,
         rc.SeriesId,
         rc.BoatClassName,
         COUNT(DISTINCT rc.CompetitorId)
FROM     #RaceCompetitors AS rc
         INNER JOIN
         dbo.Seasons AS se WITH (NOLOCK)
         ON rc.RaceDate >= se.[Start]
            AND rc.RaceDate <= se.[End]
            AND se.ClubId = @ClubId
GROUP BY se.Name, rc.SeriesId, rc.BoatClassName;

IF OBJECT_ID('tempdb..#SeasonDistinctSeriesCounts') IS NOT NULL
    DROP TABLE #SeasonDistinctSeriesCounts;

CREATE TABLE #SeasonDistinctSeriesCounts (
    SeasonName                 NVARCHAR (255)   NOT NULL,
    SeriesId                   UNIQUEIDENTIFIER NOT NULL,
    DistinctCompetitorsStarted INT           NOT NULL
);

INSERT INTO #SeasonDistinctSeriesCounts (SeasonName, SeriesId, DistinctCompetitorsStarted)
SELECT   se.Name,
         rc.SeriesId,
         COUNT(DISTINCT rc.CompetitorId)
FROM     #RaceCompetitors AS rc
         INNER JOIN
         dbo.Seasons AS se WITH (NOLOCK)
         ON rc.RaceDate >= se.[Start]
            AND rc.RaceDate <= se.[End]
            AND se.ClubId = @ClubId
GROUP BY se.Name, rc.SeriesId;

CREATE NONCLUSTERED INDEX IX_SeasonDistinctClassCounts_Season_Series_Class
    ON #SeasonDistinctClassCounts(SeasonName, SeriesId, BoatClassName);

CREATE NONCLUSTERED INDEX IX_SeasonDistinctSeriesCounts_Season_Series
    ON #SeasonDistinctSeriesCounts(SeasonName, SeriesId);

------------------------------ -- Final aggregation using #RaceStats for sums and precomputed distincts -----------------------------

SELECT   se.Name AS SeasonName,
         MIN(se.[Start]) AS SeasonStart,
         MAX(rs.SeriesName) AS SeriesName,
         CASE WHEN MIN(rs.SeriesType) = 3 THEN 'Summary' WHEN MIN(rs.SeriesType) = 1 THEN 'Standard' WHEN MIN(rs.SeriesType) = 2 THEN 'Regatta' ELSE '' END AS SeriesType,
         COUNT(DISTINCT rs.BoatClassName) AS ClassCount,
         rs.BoatClassName AS ClassName,
         COUNT(DISTINCT rs.RaceId) AS RaceCount,
         SUM(rs.CompetitorsStarted) AS CompetitorsStarted,
         COALESCE (MAX(sc.DistinctCompetitorsStarted), MAX(ss.DistinctCompetitorsStarted)) AS DistinctCompetitorsStarted,
         CASE WHEN COUNT(DISTINCT rs.RaceId) = 0 THEN 0.0 ELSE 1.0 * SUM(rs.CompetitorsStarted) / COUNT(DISTINCT rs.RaceId) END AS AverageCompetitorsPerRace,
         COUNT(DISTINCT rs.RaceDate) AS DistinctDaysRaced,
         MAX(rs.RaceDate) AS LastRace,
         MIN(rs.RaceDate) AS FirstRace
FROM     #RaceStats AS rs
         INNER JOIN
         dbo.Seasons AS se WITH (NOLOCK)
         ON rs.RaceDate >= se.[Start]
            AND rs.RaceDate <= se.[End]
            AND se.ClubId = @ClubId
         LEFT OUTER JOIN
         #SeasonDistinctClassCounts AS sc
         ON sc.SeasonName = se.Name
            AND sc.SeriesId = rs.SeriesId
            AND ((sc.BoatClassName = rs.BoatClassName)
                 OR (sc.BoatClassName IS NULL
                     AND rs.BoatClassName IS NULL))
         LEFT OUTER JOIN
         #SeasonDistinctSeriesCounts AS ss
         ON ss.SeasonName = se.Name
            AND ss.SeriesId = rs.SeriesId
WHERE    (@StartDate IS NULL
          OR rs.RaceDate >= @StartDate)
         AND (@EndDate IS NULL
              OR rs.RaceDate <= @EndDate)
GROUP BY se.Name, rs.SeriesId, ROLLUP(rs.BoatClassName)
HAVING   se.Name IS NOT NULL
         AND (COUNT(DISTINCT rs.BoatClassName) > 1
              OR rs.BoatClassName IS NOT NULL)
ORDER BY MIN(se.[Start]) DESC, MIN(rs.SeriesName), rs.BoatClassName;



------------------------------------- -- Cleanup ------------------------------


DROP TABLE #SeasonDistinctClassCounts;

DROP TABLE #SeasonDistinctSeriesCounts;

DROP TABLE #RaceCompetitors;

DROP TABLE #RaceStats;

DROP TABLE #ValidScoreCodes;

DROP TABLE #RacesWithOwningSeries;

DROP TABLE #SeriesLinks;
