--  DECLARE @SailNumber NVARCHAR(30)
--  SET @SailNumber = '2091'
--  DECLARE @ClubInitials NVARCHAR(30)
--  SET @ClubInitials = 'lhyc'


DECLARE @ClubId UNIQUEIDENTIFIER
SET @ClubId = (
    SELECT Id
    FROM Clubs
    WHERE Initials = @ClubInitials
)
DECLARE @CompetitorId UNIQUEIDENTIFIER
SET @CompetitorId = (
    SELECT top 1
        Id
    FROM Competitors
    WHERE SailNumber = @SailNumber
        AND ClubId = @ClubId
        order by IsActive desc
        )

SELECT
    c.Id AS CompetitorId,
    Seasons.Name AS SeasonName,
    Seasons.[Start] AS [SeasonStart],
    Seasons.[End] AS [SeasonEnd],
    count(distinct s.RaceId) AS RaceCount,
    AVG(CAST(s.Place AS FLOAT)) AS AverageFinishRank,
    count(DISTINCT RacedDate) AS DaysRaced,
    -- CASE WHEN RaceResults.FinisherCount = 1 THEN Null ELSE
    --    CONVERT(Decimal(7,4),RaceResults.FinisherCount - RaceResults.Place) /
    --    CONVERT(Decimal(7,4), RaceResults.FinisherCount - 1)
    --    END as percentile

    SUM(RaceResults.FinisherCount) AS RaceFinishers,
    SUM(RaceResults.FinisherCount - 1) AS BoatsRacedAgainst,
    SUM(RaceResults.FinisherCount - RaceResults.Place ) AS BoatsBeat
FROM
    Seasons
    INNER JOIN
    Competitors c
    on c.Id = @CompetitorId -- unconventional, but awesome
    AND Seasons.ClubId = @ClubId
    LEFT OUTER JOIN Races r
    ON r.[Date] >= Seasons.[Start] AND r.[Date] <= Seasons.[End]
    AND r.ClubId = @ClubId
    LEFT OUTER JOIN
    Scores s
    ON c.Id = s.CompetitorId
    and r.Id = s.RaceId
    LEFT OUTER JOIN
    (
SELECT
        r.Id,
        r.Date as RacedDate,
        COUNT(*) FinisherCount,
        racerScore.Place
    FROM
        Races r
        INNER JOIN
        Scores s
        ON s.RaceId = r.Id
        LEFT OUTER JOIN
        ScoreCodes
        ON s.Code = ScoreCodes.Name
        INNER JOIN
        Scores racerScore
        ON r.Id = racerScore.RaceId
            AND racerScore.CompetitorId = @CompetitorId
    WHERE
ISNULL(s.Code, '') = ''
        AND ISNULL(racerScore.Place,0) <> 0
    GROUP BY r.Id,
    r.Date,
racerScore.Place
) AS RaceResults
    ON RaceResults.Id = r.Id
WHERE
ISNULL(s.Code, '') = ''
    AND
    c.Id = @CompetitorId
    -- and ( Place is null or Place = 0 )
    AND seasons.ID IN (SELECT TOP 2
        Seasons.ID
    FROM Seasons
    WHERE Seasons.[Start] < GETDATE()
        AND Seasons.ClubId = @ClubId
    ORDER BY [Start] DESC)
    --Not in a series that Shouldn't be counted toward season stats
    AND (r.ID IS NULL OR r.Id NOT IN ( SELECT r2.Id
    FROM
        Races r2
        INNER JOIN SeriesRace sr2
        ON sr2.RaceId = r2.Id
        INNER JOIN Series s2
        ON sr2.SeriesId = s2.Id
    WHERE s2.ExcludeFromCompetitorStats = 1

))

GROUP BY
   c.Id,
   Seasons.Name,
   Seasons.[Start],
   Seasons.[End]
