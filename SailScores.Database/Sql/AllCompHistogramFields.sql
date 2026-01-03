--Declare @ClubId UniqueIdentifier;
--set @ClubId = (Select Id from Clubs where Initials = @ClubInitials)

SELECT
    Code,
    MAX(CASE WHEN CODE IS NULL THEN Place ELSE NULL END) as MaxPlace
FROM Scores
INNER JOIN Races   ON Scores.RaceId = Races.Id
INNER JOIN Competitors ON Scores.CompetitorId = Competitors.Id
INNER JOIN Seasons     ON Races.Date BETWEEN Seasons.[Start] AND Seasons.[End]
AND Seasons.ClubId = Races.ClubId
WHERE Races.ClubId = @ClubId
    AND Races.Id NOT IN (
        Select RaceId from SeriesRace
        inner join Series on SeriesRace.SeriesId = Series.Id
        where Series.ExcludeFromCompetitorStats = 1 AND Series.ClubId = @ClubId)
    AND ( @StartDate is null OR Races.Date >= @StartDate )
    AND ( @EndDate is null OR Races.Date <= @EndDate )
GROUP BY
    Code





