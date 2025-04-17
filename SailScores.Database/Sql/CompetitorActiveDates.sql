
SELECT
C.Id,
--c.Name,
--C.SailNumber,
MIN(R.[Date]) AS EarliestDate,
MAX(R.[Date]) AS LatestDate
FROM
Competitors AS C
left outer JOIN
Scores AS S
on C.Id = S.CompetitorId
left outer JOIN
Races AS R
on R.Id = S.RaceId
where C.ClubId = @ClubId
GROUP BY C.Id --, C.Name, C.SailNumber
--order by C.Name
