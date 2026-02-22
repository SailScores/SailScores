/****** Object:  StoredProcedure [dbo].[SS_SP_GetSeasonSummary]    Script Date: 2/17/2026 7:49:49 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO



-- =============================================
-- Author:		Jamie Fraser
-- Create date: April 30th, 2020
-- Modified:    BFeb 16, 2026 for perrformance
-- Description:	For more speed on Competitor Stats pages
-- =============================================
CREATE PROCEDURE [dbo].[SS_SP_GetSeasonSummary]
    @CompetitorId UniqueIdentifier,
    @ClubId UniqueIdentifier
AS
BEGIN
    SET NOCOUNT ON;

-- 1. Identify the 5 most recent seasons up to the most recent season the competitor competed in
-- Includes seasons the competitor didn't participate in if they fall before the most recent competed season
;WITH TargetSeasons AS (
    SELECT TOP 5
        s.Name, s.UrlName, s.[Start], s.[End]
    FROM Seasons s
    WHERE s.ClubId = @ClubId
      AND s.[Start] <= (
          -- Find the most recent season where this competitor competed
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

    -- 2. Identify Races to exclude based on Series settings
    ExcludedRaces AS (
        SELECT sr.RaceId
        FROM SeriesRace sr
        INNER JOIN Series s ON sr.SeriesId = s.Id
        WHERE s.ExcludeFromCompetitorStats = 1
    ),

    -- 3. Calculate stats for EVERY race the competitor was in
    -- We do this as a standalone set so the math is clean
    RaceStats AS (
        SELECT
            r.Id AS RaceId,
            r.Date AS RacedDate,
            racerScore.Place,
            -- Subquery here ensures we only count finishers once per race
            (SELECT COUNT(*) FROM Scores s WHERE s.RaceId = r.Id AND s.Code IS NULL) AS FinisherCount
        FROM Races r
        INNER JOIN Scores racerScore ON r.Id = racerScore.RaceId
        WHERE r.ClubId = @ClubId
          AND racerScore.CompetitorId = @CompetitorId
          AND racerScore.Place > 0
          AND NOT EXISTS (
              SELECT 1 FROM ExcludedRaces er WHERE er.RaceId = r.Id
          )
    )

    -- 4. Final Join: Map the pre-calculated race stats to the seasons
    SELECT
        ts.Name AS SeasonName,
        ts.UrlName AS SeasonUrlName,
        ts.[Start] AS [SeasonStart],
        ts.[End] AS [SeasonEnd],
        COUNT(rs.RaceId) AS RaceCount,
        AVG(CAST(rs.Place AS FLOAT)) AS AverageFinishRank,
        COUNT(DISTINCT rs.RacedDate) AS DaysRaced,
        SUM(rs.FinisherCount - 1) AS BoatsRacedAgainst,
        SUM(rs.FinisherCount - rs.Place) AS BoatsBeat,
        MAX(rs.RacedDate) AS LastRacedDate
    FROM TargetSeasons ts
    LEFT OUTER JOIN RaceStats rs
        ON rs.RacedDate >= ts.[Start]
        AND rs.RacedDate <= ts.[End]
    GROUP BY
        ts.Name, ts.UrlName, ts.[Start], ts.[End]
    ORDER BY ts.[Start] DESC;

END
GO


