
Declare @ClubId UniqueIdentifier;
set @ClubId = (Select Id from Clubs where Initials = @ClubInitials)

; With SeriesLinks As (
	Select
       Id AS ParentSeriesId,
       Id AS DescendentSeriesId
    From
	Series s
	where s.ClubId = @ClubId
	Union All
	Select s.ParentSeriesId, sl.DescendentSeriesId From dbo.SeriesToSeriesLink s
	Inner Join SeriesLinks sl On s.ChildSeriesId = sl.ParentSeriesId
),
RacesWithOwningSeries AS (
	Select
	s.ID as SeriesId,
	s.Name as SeriesName,
    s.Type as SeriesType,
	r.Id AS RaceId,
	sl.ParentSeriesId,
	r.Date as RaceDate
	from
	Races r WITH (NOLOCK)
		Inner Join SeriesRace sr WITH (NOLOCK) On r.Id = sr.RaceId
		Inner Join SeriesLinks sl WITH (NOLOCK) On sr.SeriesId = sl.DescendentSeriesId
		inner join Series s WITH (NOLOCK) on s.Id = sl.ParentSeriesId
		where r.ClubId = @ClubId
)
SELECT
	Seasons.Name AS SeasonName,
    --MAX(Seasons.UrlName) AS SeasonUrlName,
    MIN(Seasons.[Start]) AS SeasonStart,
    --SeriesId,
	MAX(SeriesName) as SeriesName,
    CASE
        WHEN MIN(SeriesType) = 3 THEN 'Summary'
        WHEN MIN(SeriesType) = 1 THEN 'Standard'
        WHEN MIN(SeriesType) = 2 THEN 'Regatta'
    ELSE '' END as SeriesType,
    COUNT(DISTINCT(bc.Name)) AS ClassCount,
    bc.Name AS ClassName,
    COUNT(DISTINCT r.RaceId) AS RaceCount,
    COUNT(DISTINCT s.Id) AS [CompetitorsStarted],
    COUNT(DISTINCT s.CompetitorId) AS [DistinctCompetitorsStarted],
    COUNT(DISTINCT s.Id) * 1.0  / ISNULL(COUNT(DISTINCT r.RaceId),1.0) AS AverageCompetitorsPerRace,
    COUNT(DISTINCT r.RaceDate) AS DistinctDaysRaced,
    MAX(r.RaceDate) AS LastRace,
    MIN(r.RaceDate) AS FirstRace
FROM RacesWithOwningSeries r
    INNER JOIN Seasons WITH (NOLOCK)
    ON r.RaceDate >= Seasons.Start AND r.RaceDate <= Seasons.[End]
        AND Seasons.ClubId = @ClubId
    INNER JOIN Scores s WITH (NOLOCK)
    ON s.RaceId = r.RaceId
        AND (s.Code IS NULL
        OR s.Code IN (SELECT ScoreCodes.Name
        FROM ScoreCodes WITH (NOLOCK)
        WHERE ClubId = @ClubId AND ScoreCodes.[Started] = 1))

    LEFT OUTER JOIN Competitors comp WITH (NOLOCK)
    ON s.CompetitorId = comp.Id
    LEFT OUTER JOIN BoatClasses bc WITH (NOLOCK)
    ON bc.Id = comp.BoatClassId
WHERE
    (@StartDate IS NULL OR r.RaceDate >= @StartDate)
    AND (@EndDate IS NULL OR r.RaceDate <= @EndDate)
GROUP BY
    Seasons.Name,
    SeriesId,
    ROLLUP (bc.Name)
HAVING Seasons.Name IS NOT NULL
AND COUNT(DISTINCT(bc.Name)) > 1 OR bc.Name IS NOT NULL
ORDER BY
    MIN(Seasons.[Start]) DESC,
    MIN(SeriesName) ,
    bc.Name

