

; WITH newestSeason AS (
Select 
Clubs.Id, Clubs.Name,
MAX(Seasons.[End]) as SeasonEnd
, (Select Id from Seasons s2 where s2.ClubId = Clubs.Id and s2.[End] = MAX(Seasons.[End])) as SeasonId
 from
Clubs
inner JOIN
Seasons
on Clubs.Id = Seasons.ClubId
where Seasons.[End] > '2022-02-01'
group BY
Clubs.Id, Clubs.Name
having MAX(Seasons.[End]) < GetDATE()
)

-- Insert into Seasons
-- (
--     Id,
--     ClubId,
--     [Start],
--     [End],
--     Name,
--     UrlName
-- )

SELECT
NewId(),
ClubId,
 DATEADD(year, 1, [Start]),
 DATEADD(year, 1, [End]),
 '2023' as Name,
 '2023' as UrlName
from Seasons
where Id in (Select SeasonId from newestSeason)
AND [Start] = '2022-01-01 00:00:00.0000000'
AND [End] = '2022-12-31 00:00:00.0000000'