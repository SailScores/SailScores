--USE SailScores

DECLARE @ClubId UNIQUEIDENTIFIER

SET @ClubId = (SELECT Id
FROM Clubs
WHERE Initials = 'LHYC')



-- SELECT Seasons.Name, COUNT(DISTINCT r.Id) as Races,
-- COUNT(DISTINCT s.Id) As [Started]
-- FROM Races r
-- left outer join Scores s
-- on s.RaceId = r.Id
-- AND (s.Code is null
-- or s.Code in (Select ScoreCodes.Name from ScoreCodes where ClubId = @ClubId and ScoreCodes.[Started] = 1))
-- INNER JOIN Seasons
--     ON r.Date >= Seasons.Start AND r.Date <= Seasons.[End]
--     and Seasons.ClubId = r.ClubId
-- WHERE r.ClubId = @ClubId

-- group by Seasons.Name


-- SELECT Seasons.Name,
--  COUNT(DISTINCT r.Id) as Races,
-- COUNT(DISTINCT s.Id) As [CompetitorsStarted],
-- COUNT(DISTINCT s.CompetitorId) As [DistinctCompetitorsStarted],
-- COUNT(DISTINCT s.Id) * 1.0  / ISNULL(COUNT(DISTINCT r.Id),1.0) as AverageCompetitorsPerRace

-- FROM Races r
-- left outer join Scores s
-- on s.RaceId = r.Id
-- AND (s.Code is null
-- or s.Code in (Select ScoreCodes.Name from ScoreCodes where ClubId = @ClubId and ScoreCodes.[Started] = 1))
-- INNER JOIN Seasons
--     ON r.Date >= Seasons.Start AND r.Date <= Seasons.[End]
--     and Seasons.ClubId = r.ClubId
-- WHERE r.ClubId = @ClubId
-- AND ISNULL(r.[State],'Raced') = 'Raced'

-- group by Seasons.Name


SELECT
c.Name,
c.Initials,
Seasons.Name as SeasonName,
Seasons.UrlName as SeasonName,
COALESCE(bc.Name, 'Total') as FleetName,
COUNT(DISTINCT r.Id) as RaceCount,
COUNT(DISTINCT s.Id) As [CompetitorsStarted],
COUNT(DISTINCT s.CompetitorId) As [DistinctCompetitorsStarted],
COUNT(DISTINCT s.Id) * 1.0  / ISNULL(COUNT(DISTINCT r.Id),1.0) as AverageCompetitorsPerRace,
MAX(r.Date) as LastRace,
MIN(r.Date) as FirstRace

FROM Races r WITH (NOLOCK)
left outer join Scores s WITH (NOLOCK)
on s.RaceId = r.Id
AND (s.Code is null
or s.Code in (Select ScoreCodes.Name from ScoreCodes WITH (NOLOCK) where ClubId = @ClubId and ScoreCodes.[Started] = 1))
INNER JOIN Seasons WITH (NOLOCK)
    ON r.Date >= Seasons.Start AND r.Date <= Seasons.[End]
    and Seasons.ClubId = r.ClubId
LEFT OUTER JOIN Competitors comp WITH (NOLOCK)
on s.CompetitorId = comp.Id
LEFT OUTER JOIN BoatClasses bc WITH (NOLOCK)
on bc.Id = comp.BoatClassId
INNER JOIN Clubs c WITH (NOLOCK)
ON c.Id = r.ClubId
WHERE r.ClubId = @ClubId
AND ISNULL(r.[State],'Raced') = 'Raced'

group by
c.Name,
c.Initials,
Seasons.[Start],
 Seasons.Name,
bc.Name WITH ROLLUP
having Seasons.Name is not null
order by 
 Seasons.Start desc,
bc.Name




-- SELECT
-- Seasons.Name,
-- w.WindDirectionDegrees,
-- w.WindDirectionString,
-- count(*) as RaceCount
-- FROM Races r
-- INNER JOIN Seasons
--     ON r.Date >= Seasons.Start AND r.Date <= Seasons.[End]
--     and Seasons.ClubId = r.ClubId
-- inner join Weather w
-- on r.WeatherId = w.Id
-- WHERE r.ClubId = @ClubId
-- AND ISNULL(r.[State],'Raced') = 'Raced'
-- group by
-- Seasons.[Start],
--  Seasons.Name ,
-- w.WindDirectionDegrees,
-- w.WindDirectionString
--  WITH ROLLUP
-- having Seasons.Name is not null
-- order by 
--  Seasons.Start desc,
-- w.WindDirectionDegrees


-- X Total # races
-- X Total starting boats
-- X Distinct Boats
-- X Average boats per race
-- Average wind speed, histogram (circular) of wind direction. ugh: how to group: NNW grouped with NW or N?
-- ??Races per month
-- Most races on a day
-- Abandoned
-- X Last Race
-- X Last Update


