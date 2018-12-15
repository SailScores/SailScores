declare @ClubName NVARCHAR(200);

set @ClubName = 'Bob'

Declare @ClubId UNIQUEIDENTIFIER;

set @ClubId = (Select Id from Clubs where Name = @ClubName)

Delete Scores
from  Scores s
inner join Races r
on r.Id = s.RaceId
where r.ClubId = @ClubId

Delete races
from Races r
where r.ClubId = @ClubId ;

Delete BoatClasses
from BoatClasses bc
inner join Competitors c
on c.BoatClassId = bc.Id
where c.ClubId = @ClubId

Delete Competitors
from Competitors c
where c.ClubId = @ClubId

Delete Fleets
where ClubId = @ClubId

;
Delete ScoreCodes
where ClubId = @ClubId

Delete SeriesRaces
from SeriesRaces
inner join Series
on Series.Id = SeriesRaces.SeriesId
where ClubId = @ClubId


Delete Series
where ClubId = @ClubId


Delete Seasons
where ClubId = @ClubId

Delete UserPermissions
where ClubId = @ClubId

Delete Clubs
where Id = @ClubId


