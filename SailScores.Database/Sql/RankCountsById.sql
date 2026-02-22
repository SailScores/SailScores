
--Declare @CompetitorId UNIQUEIDENTIFIER
--set @CompetitorId = '1E2FD196-F825-42D8-BCD2-DB9D1E2A5462'
--DECLARE @SeasonName NVARCHAR(30)
--set @SeasonName = '2019'

DECLARE @ClubId UNIQUEIDENTIFIER

DECLARE @SeasonId UNIQUEIDENTIFIER
DECLARE @SeasonName2 NVARCHAR(100)
DECLARE @SeasonStart DATETIME
Declare @SeasonEnd DateTime


SET @ClubId = (
SELECT ClubId
FROM Competitors
WHERE Id = @CompetitorId
)

Select
    @SeasonId = [Id],
    @SeasonName2 = [Name],
    @SeasonStart = [Start],
    @SeasonEnd = [End]
from Seasons
where UrlName = @SeasonUrlName
and Seasons.ClubId = @ClubId

;
WITH
Results AS
(
    SELECT
        s.CompetitorId,
        s.Place,
        s.Code,
        r.[Date]
    FROM
        Scores s
        INNER JOIN Races r
        ON r.Id = s.RaceId
    WHERE s.CompetitorId = @CompetitorId
        AND r.ClubId = @ClubId
        AND s.RaceId NOT IN ( SELECT sr2.RaceId
        FROM SeriesRace sr2 
            INNER JOIN Series s2
            ON sr2.SeriesId = s2.Id
        WHERE s2.ExcludeFromCompetitorStats = 1
        ) 
        AND r.Date >= @SeasonStart
        AND r.Date <= @SeasonEnd
),
MaxPlace AS
(
    -- Find the maximum place/rank that was achieved
    SELECT ISNULL(MAX(Place)+5, 10) AS MaxRank
    FROM Results
    WHERE Place IS NOT NULL
),
RankGenerator AS
(
    -- Generate all ranks from 1 to MaxRank
    SELECT 1 AS Rank
    UNION ALL
    SELECT Rank + 1
    FROM RankGenerator
    WHERE Rank < (SELECT MaxRank FROM MaxPlace)
),
AllRanks AS
(
    -- Select from the rank generator CTE
    SELECT Rank
    FROM RankGenerator
),
PlaceGroups AS
(
    -- Aggregate results by Place/Code
    SELECT
        ISNULL(Place, 0) AS PlaceOrCode,
        Place,
        Code,
        COUNT(*) AS Count
    FROM Results
    GROUP BY Place, Code
),
AllPlacesWithCounts AS
(
    -- Left join all ranks with actual results to include zero counts
    SELECT
        ar.Rank,
        pg.Place,
        pg.Code,
        ISNULL(pg.Count, 0) AS Count
    FROM AllRanks ar
    LEFT JOIN PlaceGroups pg ON ar.Rank = pg.Place
    UNION ALL
    -- Include special codes (DNF, DNC, etc.) that don't have a numeric place
    SELECT
        NULL,
        NULL,
        pg.Code,
        pg.Count
    FROM PlaceGroups pg
    WHERE pg.Place IS NULL
)
SELECT
    @SeasonName2 AS SeasonName,
    @SeasonStart AS SeasonStart,
    NULLIF(Rank, 0) AS Place,
    Code,
    Count
FROM AllPlacesWithCounts
ORDER BY
    CASE WHEN Rank IS NULL THEN 100 ELSE Rank END,
    Code
