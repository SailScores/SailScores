/****** Object:  StoredProcedure [dbo].[SS_SP_GetSeasonSummaryHandicap]    Script Date: 2026 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- =============================================
-- Author:      SailScores
-- Description: Returns handicap-corrected season summary stats for a competitor.
--              Uses competitor-level rating (CompetitorHandicaps) first,
--              falling back to class-level rating (ClassHandicaps).
--              Races without ElapsedTime or without a resolvable rating are excluded
--              from corrected stats. PhrfToD (SystemType=1) is not supported because
--              course distance is not stored; the SP will return zero rows for that type.
--
-- SystemType values:
--   1 = PhrfToD   (unsupported - no distance stored)
--   2 = PhrfToT   corrected = elapsed_sec * 600.0 / (600 + rating)
--   3 = Portsmouth  corrected = elapsed_sec / rating * 1000
--   4 = TimeOnTime  corrected = elapsed_sec * rating
--   5 = PortsmouthDpy  corrected = elapsed_sec / rating * 100
-- =============================================
CREATE OR ALTER PROCEDURE [dbo].[SS_SP_GetSeasonSummaryHandicap]
    @CompetitorId   UNIQUEIDENTIFIER,
    @ClubId         UNIQUEIDENTIFIER,
    @HandicapSystemId UNIQUEIDENTIFIER,
    @SystemType     INT
AS
BEGIN
    SET NOCOUNT ON;

    -- PhrfToD requires course distance which is not stored; bail out with empty result.
    IF @SystemType = 1
    BEGIN
        SELECT
            s.Name AS SeasonName,
            s.UrlName AS SeasonUrlName,
            s.[Start] AS SeasonStart,
            s.[End] AS SeasonEnd,
            0 AS CorrectedRaceCount,
            CAST(NULL AS FLOAT) AS AverageCorrectedRank,
            CAST(NULL AS INT) AS CorrectedBoatsRacedAgainst,
            CAST(NULL AS INT) AS CorrectedBoatsBeat
        FROM Seasons s
        WHERE 1 = 0;
        RETURN;
    END

    -- 1. The 5 most recent seasons up to the most recent season this competitor competed in.
    ;WITH TargetSeasons AS (
        SELECT TOP 5
            s.Name, s.UrlName, s.[Start], s.[End]
        FROM Seasons s
        WHERE s.ClubId = @ClubId
          AND s.[Start] <= (
              SELECT MAX(s2.[Start])
              FROM Seasons s2
              INNER JOIN Races r ON s2.ClubId = r.ClubId
                  AND r.Date >= s2.[Start]
                  AND r.Date <= s2.[End]
              INNER JOIN Scores sc ON r.Id = sc.RaceId
              WHERE s2.ClubId = @ClubId
                AND sc.CompetitorId = @CompetitorId
          )
          AND s.[Start] < GETDATE()
          AND s.[Start] < (SELECT MAX(Date) FROM Races WHERE ClubId = @ClubId)
        ORDER BY s.[Start] DESC
    ),

    -- 2. Races to exclude (series marked ExcludeFromCompetitorStats).
    ExcludedRaces AS (
        SELECT sr.RaceId
        FROM SeriesRace sr
        INNER JOIN Series s ON sr.SeriesId = s.Id
        WHERE s.ExcludeFromCompetitorStats = 1
    ),

    -- 3. All scores in eligible races that have ElapsedTime recorded.
    --    Resolve rating: competitor-level first, then class-level fallback.
    ScoredRaces AS (
        SELECT
            r.Id        AS RaceId,
            r.Date      AS RaceDate,
            sc.CompetitorId,
            -- Elapsed time in seconds
            DATEDIFF(SECOND, '00:00:00', sc.ElapsedTime) AS ElapsedSec,
            -- Competitor-level rating effective on race date
            (
                SELECT TOP 1 ch.Value
                FROM CompetitorHandicaps ch
                WHERE ch.CompetitorId = sc.CompetitorId
                  AND ch.HandicapSystemId = @HandicapSystemId
                  AND (ch.EffectiveFrom IS NULL OR ch.EffectiveFrom <= r.Date)
                  AND (ch.EffectiveTo   IS NULL OR ch.EffectiveTo   >= r.Date)
                ORDER BY ch.EffectiveFrom DESC
            ) AS CompRating,
            -- Class-level fallback rating effective on race date
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
        WHERE r.ClubId = @ClubId
          AND sc.ElapsedTime IS NOT NULL
          AND sc.Code IS NULL          -- only finishers
          AND NOT EXISTS (
              SELECT 1 FROM ExcludedRaces er WHERE er.RaceId = r.Id
          )
    ),

    -- 5. Resolve the effective rating and compute corrected seconds.
    WithCorrected AS (
        SELECT
            RaceId,
            RaceDate,
            CompetitorId,
            ElapsedSec,
            COALESCE(CompRating, ClassRating) AS EffectiveRating,
            CASE
                WHEN COALESCE(CompRating, ClassRating) IS NULL THEN NULL
                WHEN @SystemType = 2 THEN   -- PhrfToT
                    ElapsedSec * 600.0 / (600.0 + COALESCE(CompRating, ClassRating))
                WHEN @SystemType = 3 THEN   -- Portsmouth
                    ElapsedSec * 1000.0 / COALESCE(CompRating, ClassRating)
                WHEN @SystemType = 4 THEN   -- TimeOnTime
                    ElapsedSec * COALESCE(CompRating, ClassRating)
                WHEN @SystemType = 5 THEN   -- PortsmouthDpy
                    ElapsedSec * 100.0 / COALESCE(CompRating, ClassRating)
                ELSE NULL
            END AS CorrectedSec
        FROM ScoredRaces
    ),

    -- 6. Re-rank all competitors within each race by corrected time (asc).
    --    Only rows with a valid CorrectedSec participate in ranking.
    RaceRanks AS (
        SELECT
            RaceId,
            RaceDate,
            CompetitorId,
            CorrectedSec,
            RANK() OVER (PARTITION BY RaceId ORDER BY CorrectedSec ASC) AS CorrectedPlace,
            COUNT(*) OVER (PARTITION BY RaceId) AS FinisherCount
        FROM WithCorrected
        WHERE CorrectedSec IS NOT NULL
    ),

    -- 7. Only the rows for the target competitor.
    MyRanks AS (
        SELECT
            rr.RaceId,
            rr.RaceDate,
            rr.CorrectedPlace,
            rr.FinisherCount
        FROM RaceRanks rr
        WHERE rr.CompetitorId = @CompetitorId
    )

    -- 8. Aggregate into seasons.
    SELECT
        ts.Name         AS SeasonName,
        ts.UrlName      AS SeasonUrlName,
        ts.[Start]      AS SeasonStart,
        ts.[End]        AS SeasonEnd,
        CAST(COUNT(mr.RaceId) AS INT)                                        AS CorrectedRaceCount,
        AVG(CAST(mr.CorrectedPlace AS FLOAT))                                AS AverageCorrectedRank,
        CAST(SUM(mr.FinisherCount - 1) AS INT)                               AS CorrectedBoatsRacedAgainst,
        CAST(SUM(mr.FinisherCount - mr.CorrectedPlace) AS INT)               AS CorrectedBoatsBeat
    FROM TargetSeasons ts
    LEFT JOIN MyRanks mr
        ON mr.RaceDate >= ts.[Start]
       AND mr.RaceDate <= ts.[End]
    GROUP BY
        ts.Name, ts.UrlName, ts.[Start], ts.[End]
    ORDER BY ts.[Start] DESC;

END
GO
