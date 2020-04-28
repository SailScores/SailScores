DECLARE @SailNumber NVARCHAR(30)

SET @SailNumber = '2144'
DECLARE @ClubInitials NVARCHAR(30)
SET @ClubInitials = 'lhyc'

DECLARE @ClubId UNIQUEIDENTIFIER
SET @ClubId = (
SELECT Id
FROM Clubs
WHERE Initials = @ClubInitials
)
DECLARE @CompetitorId UNIQUEIDENTIFIER
SET @CompetitorId = (SELECT
    Id
FROM Competitors
WHERE SailNumber LIKE @SailNumber
    AND ClubId = @ClubId)

--SET @CompetitorId = '1e2fd196-f825-42d8-bcd2-db9d1e2a5462'


SELECT
    c.Name AS competitor,
    c.SailNumber,
    c.BoatName,
    Seasons.Name AS Season,
    Seasons.[Start] AS [Start],
    Seasons.[End] AS [End],
    count(r.Id) AS RaceCount,
    AVG(CAST(s.Place AS FLOAT)) as AveragePlace,
    count(DISTINCT r.Date) AS DaysRaced,
    -- CASE WHEN Code IS NULL OR Code = '' THEN CONVERT(NVARCHAR(5), s.Place) ELSE CODE END AS Result,
    -- RaceResults.Place,
    -- RaceResults.FinisherCount,
    -- RaceResults.FinisherCount - RaceResults.Place as PeopleBeat
    -- CASE WHEN RaceResults.FinisherCount = 1 THEN Null ELSE
    --    CONVERT(Decimal(7,4),RaceResults.FinisherCount - RaceResults.Place) /
    --    CONVERT(Decimal(7,4), RaceResults.FinisherCount - 1)
    --    END as percentile

    SUM(RaceResults.FinisherCount) AS Finishers,
    SUM(RaceResults.FinisherCount - 1) AS OtherFinishers,
    SUM(RaceResults.FinisherCount - RaceResults.Place ) AS BoatsBeat
-- CASE WHEN SUM(RaceResults.FinisherCount) = 1 THEN Null ELSE
--    CONVERT(Decimal(7,4),RaceResults.FinisherCount - RaceResults.Place) /
--    CONVERT(Decimal(7,4), RaceResults.FinisherCount - 1)
--    END as percentile
--, count(*) as Count
FROM
    Seasons
    LEFT OUTER JOIN
    Competitors c
    ON c.ClubId = Seasons.ClubId
    INNER JOIN Clubs
    ON c.ClubId = Clubs.Id
    LEFT OUTER JOIN
    Scores s
    ON c.Id = s.CompetitorId
    LEFT OUTER JOIN Races r
    ON r.Id = s.RaceId
        AND r.[Date] >= Seasons.[Start] AND r.[Date] <= Seasons.[End]
    LEFT OUTER JOIN
    (
SELECT
        r.Id,
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
        AND Seasons.ClubId = '23cc00a0-ba27-4fe2-8c1c-e448b1f68eba'
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
   C.Name,
   c.SailNumber,
   c.BoatName,
   Seasons.Name,
   Seasons.[Start],
   Seasons.[End]




-- Alter TABLE Series ADD  ExcludeFromCompetitorStats bit null

