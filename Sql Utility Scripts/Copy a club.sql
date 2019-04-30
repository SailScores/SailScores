
Declare @NewClubId UNIQUEIDENTIFIER
set @NewClubId = NEWID()

Declare @FromInitials NVARCHAR(20)
SET @FromInitials = 'LHYC'

-- Select *
-- from Clubs
-- where Initials = @FromInitials

Declare @FromClubId UNIQUEIDENTIFIER
SET @FromClubId = (Select Id
from Clubs
where Initials = @FromInitials)

PRINT N'COPYING CLUB'
PRINT N'From ID: ' + CONVERT(NVARCHAR(100), @FromClubId) + ' To ID: ' + CONVERT(NVARCHAR(100), @NewClubId)

INSERT into Clubs
    (
    Id,
    Name,
    Initials,
    Description,
    IsHidden,
    Url
    )
Select
    @NewClubId,
    Name + '2',
    Initials + '2',
    Description,
    1,
    url
from Clubs
where Id = @FromClubId

PRINT N'Club created. Copying permissions'

Insert into UserPermissions
    ( Id,
    UserEmail,
    ClubId,
    CanEditAllClubs)
Select Id, UserEmail, @NewClubId, CanEditAllClubs
from UserPermissions
where ClubId = @FromClubId



PRINT N'Copying Boat Classes'
Create TABLE #IdTranslation
(
    FromId UNIQUEIDENTIFIER,
    ToId UNIQUEIDENTIFIER,
    ConvertHistoricalJson BIT Null
    )

Insert into #IdTranslation
    (FromId, ToId)
Select Id, NEWID()
from BoatClasses
where ClubId = @FromClubId

Insert into BoatClasses
    (Id,
    ClubId,
    Name,
    Description)
Select trans.ToId, @NewClubId, Name, Description
from BoatClasses
    inner join #IdTranslation trans
    on BoatClasses.Id = trans.FromId
WHERE ClubId = @FromClubId

PRINT N'Copying Fleets'

Insert into #IdTranslation
    (FromId, ToId)
Select Id, NEWID()
from Fleets
where ClubId = @FromClubId

Insert Into Fleets
    (
    Id,
    ClubId,
    Name,
    [Description],
    FleetType,
    IsHidden,
    ShortName
    )
Select
    trans.ToId,
    @NewClubId, Name, Description, FleetType, IsHidden, ShortName
from Fleets
    inner join #IdTranslation trans
    on trans.FromId = Fleets.Id
where ClubId = @FromClubId

PRINT N'Copying Competitors'

Insert into #IdTranslation
    (FromId, ToId , ConvertHistoricalJson)
Select Id, NEWID() , 1
from Competitors
where ClubId = @FromClubId

Insert into Competitors
    (
    Id,
    ClubId,
    Name,
    SailNumber,
    AlternativeSailNumber,
    BoatName,
    Notes,
    BoatClassId
    )
Select
    trans.ToId,
    @NewClubId,
    Competitors.Name, SailNumber, AlternativeSailNumber, BoatName, Notes,
classTrans.ToId
from Competitors
    inner join #IdTranslation trans
    on Competitors.Id = trans.FromId
    inner join #IdTranslation classTrans
    on classTrans.FromId = Competitors.BoatClassId
where Competitors.ClubId = @FromClubId

PRINT N'Copying Competitor Fleet'
Insert into CompetitorFleet
    (CompetitorId,
    FleetId)
Select compTrans.ToId,
    fleetTrans.FromId
FROM
    CompetitorFleet cf
    inner join Fleets f
    on f.Id = cf.FleetId
    inner join #IdTranslation compTrans
    on compTrans.FromId = cf.CompetitorId
    inner join #IdTranslation fleetTrans
    on fleetTrans.FromId = cf.FleetId

PRINT N'Copying Fleet Boat Class'

Insert into FleetBoatClass
    (
    FleetId,
    BoatClassId
    )
Select
    fleetTrans.ToId,
    classTrans.ToId
FROM
    FleetBoatClass fbc
    inner join #IdTranslation fleetTrans
    on fleetTrans.FromId = fbc.FleetId
    inner join #IdTranslation classTrans
    on classTrans.FromId = fbc.BoatClassId

PRINT N'Copying Seasons'

Insert into Seasons
    (
    Id, ClubId, Name, [Start], [End]
    )
Select NEWID(), @NewClubId, Name, [Start], [End]
from Seasons
where ClubId = @FromClubId

PRINT N'Copying Series'

Insert into #IdTranslation
    (FromId, ToId, ConvertHistoricalJson)
Select Id, NEWID(), 1
from Series
where ClubId = @FromClubId

Insert into Series
    (
    Id,
    ClubId,
    Name,
    [Description],
    SeasonId
    )
Select trans.ToId, @NewClubId, series.Name, Description,
    (Select Id
    from Seasons
    where [Start] = s.Start and ClubId = @NewClubId)
from Series
    left outer join Seasons as s
    on series.SeasonId = s.Id
    inner join #IdTranslation trans
    on Series.Id = trans.FromId
where Series.ClubId = @FromClubId

PRINT N'Copying Scoring System'

Insert into ScoringSystems
    (Id, ClubId, Name, DiscardPattern)
Select NewId(), @NewClubId, Name, DiscardPattern
from ScoringSystems
where ClubId = @FromClubId


Insert into #IdTranslation
    (FromId, ToId , ConvertHistoricalJson)
Select Id, NEWID() , 1
from Races
where ClubId = @FromClubId

PRINT N'Copying Races'

Insert into Races
    (
    Id, ClubId, Name, [Date], [Order], [Description], FleetId, [State]
    )
Select trans.ToId, @NewClubId, Races.Name, [Date], [Order], Races.[Description],
    (Select Id
    from Fleets
    where ClubId = @NewClubId and Name = f.Name), [State]
from Races
    left outer join Fleets f
    on f.Id = races.FleetId
    left outer join #IdTranslation as trans
    on Races.Id = trans.FromId
where races.ClubId = @FromClubId

PRINT N'Copying Score Codes'

Insert into ScoreCodes
    (
    Id, ClubId, Name, [Description], PreserveResult, Discardable, [Started], FormulaValue, AdjustOtherScores, CameToStart, Finished, Formula,ScoreLike, ScoringSystemId )
SELECT NEWID(), @NewClubId, ScoreCodes.Name, [Description], PreserveResult, Discardable, [Started], FormulaValue, AdjustOtherScores, CameToStart, Finished, Formula, ScoreLike,
    (Select Id
    from ScoringSystems
    where ClubId = @NewClubId and Name = s.Name)
FROM ScoreCodes
    left outer join ScoringSystems s
    on s.Id = ScoreCodes.ScoringSystemId
where ScoreCodes.ClubId = @FromClubId

PRINT N'Copying Scores'

Insert into Scores
    ( Id, CompetitorId, RaceId, Place, Code)
Select NewId(),
    compTrans.ToId,
    racetrans.ToId,
    Place, Code
from Scores
    left outer join #IdTranslation raceTrans
    on raceTrans.FromId = Scores.RaceId
    left outer join #IdTranslation compTrans
    on compTrans.FromId = Scores.CompetitorId
where Scores.RaceId in (Select Id
from Races
where ClubId = @FromClubId )

PRINT N'Copying Series Race assignments'

Insert into SeriesRace
    ( RaceId, SeriesId)
Select
    raceTrans.ToId,
    seriesTrans.ToId
from SeriesRace
    inner join #IdTranslation raceTrans
    on RaceId = FromId
    inner join #IdTranslation seriesTrans
    on SeriesId = seriesTrans.FromId


PRINT N'Copying Historical result cache'

--- The big weird part:

Insert into #IdTranslation
    (fromId, ToId)
Select
    HistoricalResults.Id, NewId()
from HistoricalResults inner join Series
    on HistoricalResults.SeriesId = Series.Id
where Series.ClubId = @FromClubId

Insert into HistoricalResults
    (Id,
    SeriesId,
    IsCurrent,
    Results,
    Created)
Select
    historyTrans.ToId,
    seriestrans.ToId,
    IsCurrent,
    Results,
    Created
from HistoricalResults
    inner join #IdTranslation historyTrans
    on HistoricalResults.Id = historyTrans.FromId
    inner join #IdTranslation seriestrans
    on SeriesId = seriestrans.FromId
-- Inner join above filters to just Series translated in this execution.


DECLARE @runningCount int = 1;
DECLARE @TotalCount int;
PRINT N'Finding and Replacing Ids in cache'
declare @ErrMsg NVARCHAR(500);

DECLARE @FromId varchar(50),
   @ToId varchar(50)

SET @TotalCount = (SELECT COUNT(*)
FROM #IdTranslation  
WHERE ConvertHistoricalJson = 1  
)

DECLARE guid_cursor CURSOR FOR   
SELECT CONVERT(varchar(50), FromId), CONVERT(VARCHAR(50), ToId)  
FROM #IdTranslation  
WHERE ConvertHistoricalJson = 1  
ORDER BY FromId;  
  
OPEN guid_cursor  
  
FETCH NEXT FROM guid_cursor   
INTO @FromId, @ToId  
  
WHILE @@FETCH_STATUS = 0  
BEGIN  

  update hr
    set hr.Results = REPLACE(hr.Results, @FromId, @ToId)
    from HistoricalResults as hr
    where hr.Id in (Select ToId
    from #IdTranslation)

  SET @runningCount = @runningCount + 1;
  if(@runningCount % 10 = 0) BEGIN 
    SET @ErrMsg = CONVERT(VARCHAR(10), @runningCount) + ' / ' + CONVERT(VARCHAR(10), @TotalCount) 
    RAISERROR ( @ErrMsg, 0, 1) WITH NOWAIT
  END
    FETCH NEXT FROM guid_cursor
    INTO @FromId, @ToId
END
CLOSE guid_cursor;  
DEALLOCATE guid_cursor;







PRINT N'Dropping Id Translation table'

DROP Table #idTranslation

PRINT N'Done'

