

Select 
Clubs.Id, Clubs.Name
, DATEADD(year, 1, (Select Start from Seasons s2 where s2.ClubId = Clubs.Id and s2.[End] = MAX(Seasons.[End]))) as SeasonStart
,DATEADD(year, 1,MAX(Seasons.[End])) as SeasonEnd
--, (Select Id from Seasons s2 where s2.ClubId = Clubs.Id and s2.[End] = MAX(Seasons.[End])) as SeasonId
 from
Clubs
inner JOIN
Seasons
on Clubs.Id = Seasons.ClubId
where Seasons.[End] > '2023-10-01'
group BY
Clubs.Id, Clubs.Name
having MONTH(MAX(Seasons.[End])) = 12
AND MAX(Seasons.[End]) < GETDATE() + 10



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
Clubs.Id,
 DATEADD(year, 1, (Select Start from Seasons s2 where s2.ClubId = Clubs.Id and s2.[End] = MAX(Seasons.[End]))),
 DATEADD(year, 1,MAX(Seasons.[End])),
 '2024' as Name,
 '2024' as UrlName
FROM
Clubs
inner JOIN
Seasons
on Clubs.Id = Seasons.ClubId
where Seasons.[End] > '2023-10-01'
group BY
Clubs.Id, Clubs.Name
having MONTH(MAX(Seasons.[End])) = 12
AND MAX(Seasons.[End]) < GETDATE() + 10
AND MAX(Seasons.[End]) > GETDATE() - 30