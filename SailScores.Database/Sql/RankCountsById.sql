
--Declare @CompetitorId UNIQUEIDENTIFIER
--set @CompetitorId = '1E2FD196-F825-42D8-BCD2-DB9D1E2A5462'
--DECLARE @SeasonName NVARCHAR(30)
--set @SeasonName = '2019'

DECLARE @ClubId UNIQUEIDENTIFIER
SET @ClubId = (
SELECT ClubId
FROM Competitors
WHERE Id = @CompetitorId
)
;
WITH
Results AS
( SELECT
                s.CompetitorId,
                s.Place,
                s.Code,
                r.[Date]
            FROM
                Scores s
                INNER JOIN Races r
                ON r.Id = s.RaceId
            WHERE s.CompetitorId = @CompetitorId
                AND s.RaceId NOT IN ( SELECT r2.Id
                FROM
                    Races r2
                    INNER JOIN SeriesRace sr2
                    ON sr2.RaceId = r2.Id
                    INNER JOIN Series s2
                    ON sr2.SeriesId = s2.Id
                WHERE s2.ExcludeFromCompetitorStats = 1

) 
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
    RanksAndSeasons
    AS
    (
        SELECT
            Ranks.[Rank] AS [Rank],
            Seasons.Name,
            Seasons.Id AS SeasonId,
            Seasons.[Start] AS SeasonStart,
            Seasons.[End] AS SeasonEnd
        FROM
            Ranks CROSS JOIN Seasons
        WHERE  seasons.ID IN (SELECT TOP 2
            Seasons.ID
        FROM Seasons
        WHERE Seasons.Name = @SeasonName
            AND Seasons.ClubId = @ClubId
        ORDER BY [Start] DESC)
    ),
    JoinedWithResults
    AS
    (
        SELECT
            RanksAndSeasons.Name AS Season,
            RanksAndSeasons.SeasonStart ,
            RanksAndSeasons.Rank,
            Results.Place,
            Results.Code,
            count(results.CompetitorId) AS Count
        FROM
            RanksAndSeasons
            LEFT OUTER JOIN
            Results
            ON results.[Date] >= RanksAndSeasons.[SeasonStart] AND results.[Date] <= RanksAndSeasons.[SeasonEnd]
                AND ( results.Place = RanksAndSeasons.Rank OR ( ISNULL(Code, '') <> '' AND RanksAndSeasons.Rank =0))
        GROUP BY
 RanksAndSeasons.Name ,
 RanksAndSeasons.Rank ,
 RanksAndSeasons.SeasonStart ,
 Results.Place ,
 results.Code
    )
SELECT
    Season As SeasonName,
    SeasonStart,
    CASE WHEN Rank = 0 then null else rank END as Place,
    Code,
    Count
FROM JoinedWithResults
WHERE
 Rank <= (SELECT MAX(Place)
    FROM JoinedWithResults)
ORDER BY
  SeasonStart,
  Rank


