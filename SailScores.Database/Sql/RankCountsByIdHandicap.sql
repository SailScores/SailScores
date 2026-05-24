-- Handicap-corrected rank counts for the competitor-details chart.
-- Returns the same CompetitorRankStats shape as RankCountsById.sql, but
-- ranks competitors within each race by corrected finish time rather than
-- raw place. Only races with ElapsedTime on the competitor's score and a
-- resolvable rating (competitor-level first, class-level fallback) are
-- included; other races are silently excluded.
--
-- Parameters (passed via SqlParameter from EF context):
--   @CompetitorId   UNIQUEIDENTIFIER
--   @SeasonUrlName  NVARCHAR(100)

DECLARE @ClubId          UNIQUEIDENTIFIER
DECLARE @HandicapSystemId UNIQUEIDENTIFIER
DECLARE @SystemType      INT
DECLARE @SeasonId        UNIQUEIDENTIFIER
DECLARE @SeasonName      NVARCHAR(100)
DECLARE @SeasonStart     DATETIME
DECLARE @SeasonEnd       DATETIME

SELECT @ClubId = ClubId FROM Competitors WHERE Id = @CompetitorId

SELECT
    @SeasonId    = Id,
    @SeasonName  = [Name],
    @SeasonStart = [Start],
    @SeasonEnd   = [End]
FROM Seasons
WHERE UrlName  = @SeasonUrlName
  AND ClubId   = @ClubId

SELECT
    @HandicapSystemId = DefaultHandicapSystemId
FROM Clubs
WHERE Id = @ClubId

SELECT @SystemType = CAST(SystemType AS INT)
FROM HandicapSystems
WHERE Id = @HandicapSystemId

;WITH
ExcludedRaces AS (
    SELECT sr.RaceId
    FROM SeriesRace sr
    INNER JOIN Series s ON sr.SeriesId = s.Id
    WHERE s.ExcludeFromCompetitorStats = 1
),

CompClass AS (
    SELECT BoatClassId FROM Competitors WHERE Id = @CompetitorId
),

-- All finishers (ElapsedTime present) in eligible races this season.
ScoredRaces AS (
    SELECT
        r.Id   AS RaceId,
        r.Date AS RaceDate,
        sc.CompetitorId,
        DATEDIFF(SECOND, '00:00:00', sc.ElapsedTime) AS ElapsedSec,
        (
            SELECT TOP 1 ch.Value
            FROM CompetitorHandicaps ch
            WHERE ch.CompetitorId = sc.CompetitorId
              AND ch.HandicapSystemId = @HandicapSystemId
              AND (ch.EffectiveFrom IS NULL OR ch.EffectiveFrom <= r.Date)
              AND (ch.EffectiveTo   IS NULL OR ch.EffectiveTo   >= r.Date)
            ORDER BY ch.EffectiveFrom DESC
        ) AS CompRating,
        (
            SELECT TOP 1 clh.Value
            FROM ClassHandicaps clh
            INNER JOIN Competitors c ON c.BoatClassId = clh.BoatClassId
            WHERE c.Id = sc.CompetitorId
              AND clh.HandicapSystemId = @HandicapSystemId
              AND (clh.EffectiveFrom IS NULL OR clh.EffectiveFrom <= r.Date)
              AND (clh.EffectiveTo   IS NULL OR clh.EffectiveTo   >= r.Date)
            ORDER BY clh.EffectiveFrom DESC
        ) AS ClassRating
    FROM Races r
    INNER JOIN Scores sc ON r.Id = sc.RaceId
    WHERE r.ClubId   = @ClubId
      AND r.Date >= @SeasonStart
      AND r.Date <= @SeasonEnd
      AND sc.ElapsedTime IS NOT NULL
      AND sc.Code IS NULL
      AND r.Id NOT IN (SELECT RaceId FROM ExcludedRaces)
),

WithCorrected AS (
    SELECT
        RaceId,
        CompetitorId,
        CASE
            WHEN COALESCE(CompRating, ClassRating) IS NULL THEN NULL
            WHEN @SystemType = 2 THEN ElapsedSec * 600.0 / (600.0 + COALESCE(CompRating, ClassRating))
            WHEN @SystemType = 3 THEN ElapsedSec * 1000.0 / COALESCE(CompRating, ClassRating)
            WHEN @SystemType = 4 THEN ElapsedSec * COALESCE(CompRating, ClassRating)
            WHEN @SystemType = 5 THEN ElapsedSec * 100.0 / COALESCE(CompRating, ClassRating)
            ELSE NULL
        END AS CorrectedSec
    FROM ScoredRaces
),

RaceRanks AS (
    SELECT
        RaceId,
        CompetitorId,
        RANK() OVER (PARTITION BY RaceId ORDER BY CorrectedSec ASC) AS CorrectedPlace
    FROM WithCorrected
    WHERE CorrectedSec IS NOT NULL
),

MyPlaces AS (
    SELECT CorrectedPlace AS Place
    FROM RaceRanks
    WHERE CompetitorId = @CompetitorId
),

MaxPlace AS (
    SELECT ISNULL(MAX(Place) + 5, 10) AS MaxRank FROM MyPlaces
),

RankGenerator AS (
    SELECT 1 AS Rank
    UNION ALL
    SELECT Rank + 1 FROM RankGenerator
    WHERE Rank < (SELECT MaxRank FROM MaxPlace)
),

PlaceGroups AS (
    SELECT Place, COUNT(*) AS Count
    FROM MyPlaces
    GROUP BY Place
),

AllPlacesWithCounts AS (
    SELECT rg.Rank, pg.Place, ISNULL(pg.Count, 0) AS Count
    FROM RankGenerator rg
    LEFT JOIN PlaceGroups pg ON rg.Rank = pg.Place
)

SELECT
    @SeasonName AS SeasonName,
    @SeasonStart AS SeasonStart,
    NULLIF(Rank, 0) AS Place,
    CAST(NULL AS NVARCHAR(10)) AS Code,
    Count
FROM AllPlacesWithCounts
ORDER BY Rank
