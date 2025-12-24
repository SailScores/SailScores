--Declare @ClubId UniqueIdentifier;
--set @ClubId = (Select Id from Clubs where Initials = @ClubInitials)

; WITH

base AS (
    SELECT
        CompetitorId,
        Competitors.Name AS CompetitorName,
        Competitors.SailNumber,
        Seasons.Id as SeasonId,
        Seasons.Name AS SeasonName,
        Scores.RaceId,
        Races.Date,
        Code,
        Place
        -- CASE 
        --     WHEN Code IS NOT NULL THEN Code
        --     ELSE CONVERT(nvarchar(10), Place)
        -- END AS ScoreType
    FROM Scores
    INNER JOIN Races   ON Scores.RaceId = Races.Id
    INNER JOIN Competitors ON Scores.CompetitorId = Competitors.Id
INNER JOIN Seasons     ON Races.Date BETWEEN Seasons.[Start] AND Seasons.[End]
AND Seasons.ClubId = Races.ClubId
    WHERE Races.ClubId = @ClubId
    AND Races.Id NOT IN (Select RaceId from SeriesRace
    inner join Series on SeriesRace.SeriesId = Series.Id
    where Series.ExcludeFromCompetitorStats = 1 AND Series.ClubId = @ClubId)
    AND ( @StartDate is null OR Races.Date >= @StartDate )
    AND ( @EndDate is null OR Races.Date <= @EndDate )
),

-- Race-level counts (one row per race)
race_counts AS (
    SELECT
        CompetitorName,
        SailNumber,
        SeasonName,
        CompetitorId,
        SeasonId,
        Code,
        Place,
--        ScoreType,
        'Races' AS AggregationType
    FROM base
),
-- Date-level counts (one row per date)
date_counts AS (
    SELECT DISTINCT  -- DISTINCT IS DOING THE GROUPING BY DATE
        CompetitorName,
        SailNumber,
        SeasonName,
        CompetitorId,
        SeasonId,
        Code,
        Place,
        --ScoreType,
        'Days' AS AggregationType,
        Date
    FROM base
),
combined AS (
    SELECT
        CompetitorName,
        CompetitorId,
        SailNumber,
        SeasonName,
        Code,
        Place,
        AggregationType
    FROM race_counts
    UNION ALL
    SELECT
        CompetitorName,
        CompetitorId,
        SailNumber,
        SeasonName,
        Code,
        Place,
        AggregationType
    FROM date_counts
)

SELECT 
    CompetitorName,
    CompetitorId,
    SailNumber,
    SeasonName,
    --SeasonId,
    AggregationType
    ,
    Code,
    Place,
    COUNT(*) AS CountOfDistinct
FROM combined
GROUP BY
    CompetitorName,
    CompetitorId,
    SailNumber,
    SeasonName,
    AggregationType,
    Code,
    Place
ORDER BY
    CompetitorName,
    CompetitorId,
    SailNumber,
    SeasonName desc,
    AggregationType,
    Code,
    Place
;




