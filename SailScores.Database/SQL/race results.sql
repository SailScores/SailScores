
Declare @SailNumber NVARCHAR(30)
set @SailNumber = '2644'
Declare @ClubInitials nvarchar(30)
set @ClubInitials = 'lhyc'


Declare @ClubId UNIQUEIDENTIFIER
set @ClubId = (
Select Id
From Clubs where Initials = @ClubInitials
)
DECLARE @CompetitorId UNIQUEIDENTIFIER
set @CompetitorId = (SELECT
    Id
FROM Competitors
WHERE SailNumber LIKE @SailNumber
and ClubId = @ClubId)

;
With Ranks as (
    Select 1 as Rank
 UNION ALL 
    select Rank +1
    from Ranks
    where Ranks.Rank < 100
),
RanksWithNull AS
( select null as Rank
UNIon all 
Select * from Ranks),
RanksAndSeasons AS (
    Select
    RanksWithNull.Rank as Rank,
    Seasons.Name,
    Seasons.Id as SeasonId,
    Seasons.[Start] as SeasonStart,
    Seasons.[End] as SeasonEnd
    from
    RanksWithNull cross join Seasons
    where  seasons.ID IN (SELECT TOP 2
        Seasons.ID
    FROM Seasons
    WHERE Seasons.[Start] < GETDATE()
        AND Seasons.ClubId = @ClubId
    ORDER BY [Start] DESC)
),
JoinedWithResults as (
 Select
 RanksAndSeasons.Name as Season,
 RanksAndSeasons.SeasonStart ,
 RanksAndSeasons.Rank,
 Results.Place,
 Results.Code,
count(results.CompetitorId) as Count
 from
 RanksAndSeasons
 LEFT outer join
 ( Select
 s.CompetitorId,
 s.Place,
 s.Code,
 r.[Date]
 FROM
   Scores s
 left outer join Races r
 on r.Id = s.RaceId
 where s.CompetitorId = @CompetitorId
 AND (s.RaceId IS NULL OR s.RaceId NOT IN ( SELECT r2.Id
    FROM
        Races r2
        INNER JOIN SeriesRace sr2
        ON sr2.RaceId = r2.Id
        INNER JOIN Series s2
        ON sr2.SeriesId = s2.Id
    WHERE s2.ExcludeFromCompetitorStats = 1

) )
 ) as results
 on results.[Date] >= RanksAndSeasons.[SeasonStart] and results.[Date] <= RanksAndSeasons.[SeasonEnd]
and ( results.Place = RanksAndSeasons.Rank OR ( ISNULL(Code, '') <> '' AND RanksAndSeasons.Rank is null))

  
 group BY
 RanksAndSeasons.Name ,
 RanksAndSeasons.Rank ,
 RanksAndSeasons.SeasonStart ,
 Results.Place ,
 results.Code
)
Select
*
from JoinedWithResults
where
 Rank <= (Select MAX(Place) from JoinedWithResults)
or Rank is null
order by SeasonStart,
Rank


