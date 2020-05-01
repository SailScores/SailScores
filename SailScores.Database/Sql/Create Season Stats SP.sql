-- ================================================
-- Template generated from Template Explorer using:
-- Create Procedure (New Menu).SQL
--
-- Use the Specify Values for Template Parameters 
-- command (Ctrl-Shift-M) to fill in the parameter 
-- values below.
--
-- This block of comments will not be included in
-- the definition of the procedure.
-- ================================================
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		Jamie Fraser
-- Create date: April 30th, 2020
-- Description:	For more speed on Competitor Stats pages
-- =============================================
CREATE PROCEDURE SS_SP_GetSeasonSummary 
	-- Add the parameters for the stored procedure here
	@SailNumber NVARCHAR(30), 
	@ClubInitials NVARCHAR(30)
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;
	
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
    Seasons.Name AS SeasonName,
    Seasons.[Start] AS [SeasonStart],
    Seasons.[End] AS [SeasonEnd],
    count(distinct RaceResults.Id) AS RaceCount,
    AVG(CAST(RaceResults.Place AS FLOAT)) AS AverageFinishRank,
    count(DISTINCT RacedDate) AS DaysRaced,
    SUM(RaceResults.FinisherCount - 1) AS BoatsRacedAgainst,
    SUM(RaceResults.FinisherCount - RaceResults.Place ) AS BoatsBeat
FROM
(SELECT TOP 2
        *
    FROM Seasons
    WHERE Seasons.[Start] < GETDATE()
        AND Seasons.ClubId = @ClubId
    ORDER BY [Start] DESC) AS Seasons
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
		AND r.ClubId = @ClubId
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
    GROUP BY r.Id,
    r.Date,
racerScore.Place
) AS RaceResults
    ON 
	RaceResults.RacedDate >= Seasons.[Start] AND RaceResults.RacedDate <= Seasons.[End]
GROUP BY
   Seasons.Name,
   Seasons.[Start],
   Seasons.[End]

END
GO
