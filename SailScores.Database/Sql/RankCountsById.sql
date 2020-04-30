
--Declare @SailNumber NVARCHAR(30)
--set @SailNumber = '2169'
--Declare @ClubInitials nvarchar(30)
--set @ClubInitials = 'lhyc'

DECLARE @ClubId UNIQUEIDENTIFIER
SET @ClubId = (
SELECT ClubId
FROM Competitors
WHERE Id = @CompetitorId
)
;
WITH
    Ranks
    AS
    (
                    SELECT 1 AS Rank
        UNION ALL
            SELECT Rank +1
            FROM Ranks
            WHERE Ranks.Rank < 100
    ),
    RanksWithNull
    AS
    (
                    SELECT NULL AS Rank
        UNION ALL
            SELECT *
            FROM Ranks
    ),
    RanksAndSeasons
    AS
    (
        SELECT
            RanksWithNull.Rank AS Rank,
            Seasons.Name,
            Seasons.Id AS SeasonId,
            Seasons.[Start] AS SeasonStart,
            Seasons.[End] AS SeasonEnd
        FROM
            RanksWithNull CROSS JOIN Seasons
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
            ( SELECT
                s.CompetitorId,
                s.Place,
                s.Code,
                r.[Date]
            FROM
                Scores s
                LEFT OUTER JOIN Races r
                ON r.Id = s.RaceId
            WHERE s.CompetitorId = @CompetitorId
                AND (s.RaceId IS NULL OR s.RaceId NOT IN ( SELECT r2.Id
                FROM
                    Races r2
                    INNER JOIN SeriesRace sr2
                    ON sr2.RaceId = r2.Id
                    INNER JOIN Series s2
                    ON sr2.SeriesId = s2.Id
                WHERE s2.ExcludeFromCompetitorStats = 1

) )
 ) AS results
            ON results.[Date] >= RanksAndSeasons.[SeasonStart] AND results.[Date] <= RanksAndSeasons.[SeasonEnd]
                AND ( results.Place = RanksAndSeasons.Rank OR ( ISNULL(Code, '') <> '' AND RanksAndSeasons.Rank IS NULL))


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
    Rank as Place,
    Code,
    Count
FROM JoinedWithResults
WHERE
 Rank <= (SELECT MAX(Place)
    FROM JoinedWithResults)
    OR Rank IS NULL
ORDER BY
  SeasonStart,
  Rank


