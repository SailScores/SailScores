Declare @ClubId UniqueIdentifier;
set @ClubId = (Select Id from Clubs where Initials = @ClubInitials)


SELECT
    c.Name AS ClubName,
    c.Initials AS ClubInitials,
    Seasons.Name AS SeasonName,
    MAX(Seasons.UrlName) AS SeasonUrlName,
    MIN(Seasons.[Start]) AS SeasonStart,
    bc.Name AS ClassName,
    COUNT(DISTINCT r.Id) AS RaceCount,
    COUNT(DISTINCT s.Id) AS [CompetitorsStarted],
    COUNT(DISTINCT s.CompetitorId) AS [DistinctCompetitorsStarted],
    COUNT(DISTINCT s.Id) * 1.0  / ISNULL(COUNT(DISTINCT r.Id),1.0) AS AverageCompetitorsPerRace,
    COUNT(DISTINCT r.Date) AS DistinctDaysRaced,
    MAX(r.Date) AS LastRace,
    MIN(r.Date) AS FirstRace

FROM Races r WITH (NOLOCK)
    INNER JOIN Scores s WITH (NOLOCK)
    ON s.RaceId = r.Id
        AND (s.Code IS NULL
        OR s.Code IN (SELECT ScoreCodes.Name
        FROM ScoreCodes WITH (NOLOCK)
        WHERE ClubId = @ClubId AND ScoreCodes.[Started] = 1))
    INNER JOIN Seasons WITH (NOLOCK)
    ON r.Date >= Seasons.Start AND r.Date <= Seasons.[End]
        AND Seasons.ClubId = r.ClubId
    LEFT OUTER JOIN Competitors comp WITH (NOLOCK)
    ON s.CompetitorId = comp.Id
    LEFT OUTER JOIN BoatClasses bc WITH (NOLOCK)
    ON bc.Id = comp.BoatClassId
    INNER JOIN Clubs c WITH (NOLOCK)
    ON c.Id = r.ClubId
WHERE c.Id = @ClubId
    AND ISNULL(r.[State],'Raced') = 'Raced'

GROUP BY
c.Name,
c.Initials,
Seasons.Name,
bc.Name WITH ROLLUP
HAVING Seasons.Name IS NOT NULL
ORDER BY 
MIN(Seasons.[Start]) DESC,
bc.Name

