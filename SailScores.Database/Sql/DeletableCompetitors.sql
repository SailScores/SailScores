
SELECT
C.Id,
--c.Name,
--C.SailNumber,
CASE WHEN MIN(R.[Date]) IS NULL THEN CAST(1 as BIT) ELSE CAST(0 AS BIT) END as IsDeletable,
'Raced ' + FORMAT ( MIN(R.[Date]), 'MMM d yyyy') + ' - ' + FORMAT(MAX(r.[Date]), 'MMM d yyyy')  as Reason
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
