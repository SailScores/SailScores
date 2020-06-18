
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
where Name = @SeasonName
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
 ) ,
    Ranks
    AS
    (
       SELECT 0 AS [Rank]

        UNION ALL
            SELECT [Rank] +1
            FROM Ranks
            WHERE Ranks.[Rank] < 100
    ),
    JoinedWithResults
    AS
    (
        SELECT
            Ranks.Rank,
            Results.Place,
            Results.Code,
            count(results.CompetitorId) AS Count
        FROM
            Ranks
            LEFT OUTER JOIN
            Results
            ON ( results.Place = Ranks.Rank OR ( ISNULL(Code, '') <> '' AND Ranks.Rank =0))
        GROUP BY
            Ranks.Rank ,
            Results.Place ,
            results.Code
    )
SELECT
    @SeasonName2 As SeasonName ,
    @SeasonStart AS SeasonStart ,
    CASE WHEN Rank = 0 then null else rank END as Place ,
    Code ,
    Count
FROM JoinedWithResults
WHERE
 Rank <= (SELECT MAX(Place)
    FROM JoinedWithResults)
ORDER BY
  Rank
