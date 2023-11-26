
--    Select * from Clubs

DECLARE @NewClubId UNIQUEIDENTIFIER
SET @NewClubId = NEWID()

DECLARE @FromInitials NVARCHAR(20)
SET @FromInitials = 'TYC'

DECLARE @ToInitials NVARCHAR(20)
SET @ToInitials = 'TYC2'

-- Select * from Clubs
-- where Initials = @FromInitials

DECLARE @FromClubId UNIQUEIDENTIFIER
SET @FromClubId = (SELECT Id
FROM Clubs
WHERE Initials = @FromInitials)

PRINT N'COPYING CLUB'
PRINT N'From ID: ' + CONVERT(NVARCHAR(100), @FromClubId) + ' To ID: ' + CONVERT(NVARCHAR(100), @NewClubId)

INSERT INTO Clubs
    (
    Id,
    Name,
    Initials,
    Description,
    IsHidden,
    Url,
    Locale,
    ShowClubInResults
    )
SELECT
    @NewClubId,
    Name,
    @ToInitials,
    Description,
    1,
    url,
    Locale,
    ShowClubInResults
FROM Clubs
WHERE Id = @FromClubId

PRINT N'Club created. Copying permissions'

INSERT INTO UserPermissions
    ( Id,
    UserEmail,
    ClubId,
    CanEditAllClubs)
SELECT NEWID(), UserEmail, @NewClubId, CanEditAllClubs
FROM UserPermissions
WHERE ClubId = @FromClubId


CREATE TABLE #IdTranslation
(
    FromId UNIQUEIDENTIFIER,
    ToId UNIQUEIDENTIFIER,
    ConvertHistoricalJson BIT NULL
)

PRINT N'Club created. Copying weather settings'
INSERT INTO #IdTranslation
    (FromId, ToId)
SELECT Id, NEWID()
FROM WeatherSettings
WHERE Id = (SELECT WeatherSettingsId
FROM Clubs
WHERE Id = @FromClubId)


INSERT INTO WeatherSettings
    (Id, Latitude, Longitude, TemperatureUnits, WindSpeedUnits)
SELECT #IdTranslation.ToId, Latitude, Longitude, TemperatureUnits, WindSpeedUnits

FROM WeatherSettings
INNER JOIN #IdTranslation
ON WeatherSettings.Id = #IdTranslation.FromId


UPDATE Clubs
SET WeatherSettingsId = (SELECT #IdTranslation.ToId
FROM WeatherSettings ws2
    INNER JOIN #IdTranslation
    ON ws2.Id = #IdTranslation.FromId
WHERE Id = (SELECT WeatherSettingsId
FROM Clubs c2
WHERE c2.Id = @FromClubId))
WHERE Clubs.Id = @NewClubId




PRINT N'Copying Boat Classes'

INSERT INTO #IdTranslation
    (FromId, ToId)
SELECT Id, NEWID()
FROM BoatClasses
WHERE ClubId = @FromClubId

INSERT INTO BoatClasses
    (Id,
    ClubId,
    Name,
    Description)
SELECT trans.ToId, @NewClubId, Name, Description
FROM BoatClasses
    INNER JOIN #IdTranslation trans
    ON BoatClasses.Id = trans.FromId
WHERE ClubId = @FromClubId

PRINT N'Copying Fleets'

INSERT INTO #IdTranslation
    (FromId, ToId)
SELECT Id, NEWID()
FROM Fleets
WHERE ClubId = @FromClubId

INSERT INTO Fleets
    (
    Id,
    ClubId,
    Name,
    [Description],
    FleetType,
    IsHidden,
    ShortName,
    NickName,
    IsActive
    )
SELECT
    trans.ToId,
    @NewClubId, Name, Description, FleetType, IsHidden, ShortName,
    NickName,
    IsActive
FROM Fleets
    INNER JOIN #IdTranslation trans
    ON trans.FromId = Fleets.Id
WHERE ClubId = @FromClubId

PRINT N'Copying Competitors'

INSERT INTO #IdTranslation
    (FromId, ToId , ConvertHistoricalJson)
SELECT Id, NEWID() , 1
FROM Competitors
WHERE ClubId = @FromClubId

INSERT INTO Competitors
    (
    Id,
    ClubId,
    Name,
    SailNumber,
    AlternativeSailNumber,
    BoatName,
    Notes,
    BoatClassId,
    IsActive,
    HomeClubName
    )
SELECT
    trans.ToId,
    @NewClubId,
    Competitors.Name, SailNumber, AlternativeSailNumber, BoatName, Notes,
    classTrans.ToId,
    IsActive,
    HomeClubName
FROM Competitors
    INNER JOIN #IdTranslation trans
    ON Competitors.Id = trans.FromId
    INNER JOIN #IdTranslation classTrans
    ON classTrans.FromId = Competitors.BoatClassId
WHERE Competitors.ClubId = @FromClubId

PRINT N'Copying Competitor Fleet'
INSERT INTO CompetitorFleet
    (CompetitorId,
    FleetId)
SELECT compTrans.ToId,
    fleetTrans.FromId
FROM
    CompetitorFleet cf
    INNER JOIN Fleets f
    ON f.Id = cf.FleetId
    INNER JOIN #IdTranslation compTrans
    ON compTrans.FromId = cf.CompetitorId
    INNER JOIN #IdTranslation fleetTrans
    ON fleetTrans.FromId = cf.FleetId

PRINT N'Copying Fleet Boat Class'

INSERT INTO FleetBoatClass
    (
    FleetId,
    BoatClassId
    )
SELECT
    fleetTrans.ToId,
    classTrans.ToId
FROM
    FleetBoatClass fbc
    INNER JOIN #IdTranslation fleetTrans
    ON fleetTrans.FromId = fbc.FleetId
    INNER JOIN #IdTranslation classTrans
    ON classTrans.FromId = fbc.BoatClassId

PRINT N'Copying Seasons'

INSERT INTO Seasons
    (
    Id, ClubId, Name, [Start], [End],
    UrlName
    )
SELECT NEWID(), @NewClubId, Name, [Start], [End],
UrlName
FROM Seasons
WHERE ClubId = @FromClubId



PRINT N'Copying ScoringSystems'

INSERT INTO #IdTranslation
    (FromId, ToId , ConvertHistoricalJson)
SELECT Id, NEWID() , 1
FROM ScoringSystems
WHERE ClubId = @FromClubId

INSERT INTO ScoringSystems
    (
    Id, ClubId, Name, [DiscardPattern], [ParentSystemId], [ParticipationPercent]
    )
SELECT trans.ToId, @NewClubId, ss.Name, [DiscardPattern], [ParentSystemId], [ParticipationPercent]
FROM ScoringSystems ss

    LEFT OUTER JOIN #IdTranslation AS trans
    ON ss.Id = trans.FromId
WHERE ss.ClubId = @FromClubId


UPDATE Clubs
SET DefaultScoringSystemId = (SELECT #IdTranslation.ToId
FROM ScoringSystems ss2
    INNER JOIN #IdTranslation
    ON ss2.Id = #IdTranslation.FromId
WHERE Id = (SELECT DefaultScoringSystemId
FROM Clubs c2
WHERE c2.Id = @FromClubId))
WHERE Clubs.Id = @NewClubId


PRINT N'Copying Score Codes'

INSERT INTO ScoreCodes
    (
    Id,
    Name, [Description],
    PreserveResult, Discardable,
    [Started], FormulaValue,
    AdjustOtherScores, CameToStart,
    Finished, Formula,
    ScoreLike, ScoringSystemId )
SELECT NEWID(), ScoreCodes.Name, [Description], PreserveResult, Discardable, [Started], FormulaValue,
    AdjustOtherScores, CameToStart, Finished, Formula, ScoreLike,
    (SELECT Id
    FROM ScoringSystems
    WHERE ClubId = @NewClubId AND Name = s.Name)
FROM ScoreCodes
    INNER JOIN ScoringSystems s
    ON s.Id = ScoreCodes.ScoringSystemId
WHERE s.ClubId = @FromClubId



PRINT N'Copying Series'

INSERT INTO #IdTranslation
    (FromId, ToId, ConvertHistoricalJson)
SELECT Id, NEWID(), 1
FROM Series
WHERE ClubId = @FromClubId

INSERT INTO Series
    (
    Id,
    ClubId,
    Name,
    [Description],
    SeasonId,
    IsImportantSeries,
    UpdatedDateUtc,
    ScoringSystemId,
    ResultsLocked,
TrendOption,
FleetId,
PreferAlternativeSailNumbers,
UrlName,
ExcludeFromCompetitorStats
    )
SELECT trans.ToId, @NewClubId, series.Name, Description,
    (SELECT Id
    FROM Seasons
    WHERE [Start] = s.Start AND ClubId = @NewClubId),
    IsImportantSeries,
    UpdatedDateUtc,
    sstrans.ToId,
    ResultsLocked,
TrendOption,
FleetId,
PreferAlternativeSailNumbers,
Series.UrlName,
ExcludeFromCompetitorStats
FROM Series
    LEFT OUTER JOIN Seasons AS s
    ON series.SeasonId = s.Id
    INNER JOIN #IdTranslation trans
    ON Series.Id = trans.FromId
    left outer join #IdTranslation AS sstrans
    on sstrans.FromId = series.ScoringSystemId
WHERE Series.ClubId = @FromClubId



PRINT N'Copying Race Weather'

INSERT INTO #IdTranslation
    (FromId, ToId , ConvertHistoricalJson)
SELECT Id, NEWID() , 1
FROM Weather
WHERE Id in (Select WeatherId from Races where ClubId = @FromClubId)

INSERT INTO Weather
    (  [Id]
      ,[Description]
      ,[Icon]
      ,[TemperatureString]
      ,[TemperatureDegreesKelvin]
      ,[WindSpeedString]
      ,[WindSpeedMeterPerSecond]
      ,[WindDirectionString]
      ,[WindDirectionDegrees]
      ,[WindGustString]
      ,[WindGustMeterPerSecond]
      ,[Humidity]
      ,[CloudCoverPercent]
      ,[CreatedDateUtc]
      )
SELECT
    trans.ToId
      ,[Description]
      ,[Icon]
      ,[TemperatureString]
      ,[TemperatureDegreesKelvin]
      ,[WindSpeedString]
      ,[WindSpeedMeterPerSecond]
      ,[WindDirectionString]
      ,[WindDirectionDegrees]
      ,[WindGustString]
      ,[WindGustMeterPerSecond]
      ,[Humidity]
      ,[CloudCoverPercent]
      ,[CreatedDateUtc]
FROM Weather
    INNER JOIN #IdTranslation trans
    ON Id = trans.FromId



INSERT INTO #IdTranslation
    (FromId, ToId , ConvertHistoricalJson)
SELECT Id, NEWID() , 1
FROM Races
WHERE ClubId = @FromClubId

PRINT N'Copying Races'

INSERT INTO Races
    (
    Id, ClubId, Name, [Date], [Order], [Description], FleetId, [State], TrackingUrl, UpdatedDateUtc, WeatherId
    )
SELECT trans.ToId, @NewClubId, Races.Name, [Date], [Order], Races.[Description],
    (SELECT Id
    FROM Fleets
    WHERE ClubId = @NewClubId AND Name = f.Name), [State],
    TrackingUrl, UpdatedDateUtc,
    weathertrans.ToId
FROM Races
    LEFT OUTER JOIN Fleets f
    ON f.Id = races.FleetId
    LEFT OUTER JOIN #IdTranslation AS trans
    ON Races.Id = trans.FromId
    left outer join #IdTranslation AS weathertrans
    on weathertrans.FromId = races.WeatherId
WHERE races.ClubId = @FromClubId


PRINT N'Copying Scores'

INSERT INTO Scores
    ( Id, CompetitorId, RaceId, Place, Code, CodePoints)
SELECT NewId(),
    compTrans.ToId,
    racetrans.ToId,
    Place, Code,
    CodePoints
FROM Scores
    LEFT OUTER JOIN #IdTranslation raceTrans
    ON raceTrans.FromId = Scores.RaceId
    LEFT OUTER JOIN #IdTranslation compTrans
    ON compTrans.FromId = Scores.CompetitorId
WHERE Scores.RaceId IN (SELECT Id
FROM Races
WHERE ClubId = @FromClubId )

PRINT N'Copying Series Race assignments'

INSERT INTO SeriesRace
    ( RaceId, SeriesId)
SELECT
    raceTrans.ToId,
    seriesTrans.ToId
FROM SeriesRace
    INNER JOIN #IdTranslation raceTrans
    ON RaceId = FromId
    INNER JOIN #IdTranslation seriesTrans
    ON SeriesId = seriesTrans.FromId




PRINT N'Copying Historical result cache'

--- The big weird part:

INSERT INTO #IdTranslation
    (fromId, ToId)
SELECT
    HistoricalResults.Id, NewId()
FROM HistoricalResults INNER JOIN Series
    ON HistoricalResults.SeriesId = Series.Id
WHERE Series.ClubId = @FromClubId

INSERT INTO HistoricalResults
    (Id,
    SeriesId,
    IsCurrent,
    Results,
    Created)
SELECT
    historyTrans.ToId,
    seriestrans.ToId,
    IsCurrent,
    Results,
    Created
FROM HistoricalResults
    INNER JOIN #IdTranslation historyTrans
    ON HistoricalResults.Id = historyTrans.FromId
    INNER JOIN #IdTranslation seriestrans
    ON SeriesId = seriestrans.FromId
-- Inner join above filters to just Series translated in this execution.


DECLARE @runningCount INT = 1;
DECLARE @TotalCount INT;
PRINT N'Finding and Replacing Ids in cache'
DECLARE @ErrMsg NVARCHAR(500);

DECLARE @FromId VARCHAR(50),
   @ToId VARCHAR(50)

SET @TotalCount = (SELECT COUNT(*)
FROM #IdTranslation
WHERE ConvertHistoricalJson = 1  
)

DECLARE guid_cursor CURSOR FOR   
SELECT CONVERT(VARCHAR(50), FromId), CONVERT(VARCHAR(50), ToId)
FROM #IdTranslation
WHERE ConvertHistoricalJson = 1
ORDER BY FromId;

OPEN guid_cursor

FETCH NEXT FROM guid_cursor   
INTO @FromId, @ToId

WHILE @@FETCH_STATUS = 0  
BEGIN

    UPDATE hr
    SET hr.Results = REPLACE(hr.Results, @FromId, @ToId)
    FROM HistoricalResults AS hr
    WHERE hr.Id IN (SELECT ToId
    FROM #IdTranslation)

    SET @runningCount = @runningCount + 1;
    IF(@runningCount % 10 = 0) BEGIN
        SET @ErrMsg = CONVERT(VARCHAR(10), @runningCount) + ' / ' + CONVERT(VARCHAR(10), @TotalCount)
        RAISERROR ( @ErrMsg, 0, 1) WITH NOWAIT
    END
    FETCH NEXT FROM guid_cursor
    INTO @FromId, @ToId
END
CLOSE guid_cursor;
DEALLOCATE guid_cursor;



INSERT INTO #IdTranslation
    (fromId, ToId)
SELECT
    SeriesChartResults.Id, NewId()
FROM SeriesChartResults INNER JOIN Series
    ON SeriesChartResults.SeriesId = Series.Id
WHERE Series.ClubId = @FromClubId

INSERT INTO SeriesChartResults
    (Id,
    SeriesId,
    IsCurrent,
    Results,
    Created)
SELECT
    historyTrans.ToId,
    seriestrans.ToId,
    IsCurrent,
    Results,
    Created
FROM SeriesChartResults
    INNER JOIN #IdTranslation historyTrans
    ON SeriesChartResults.Id = historyTrans.FromId
    INNER JOIN #IdTranslation seriestrans
    ON SeriesId = seriestrans.FromId

DECLARE guid_cursor CURSOR FOR   
SELECT CONVERT(VARCHAR(50), FromId), CONVERT(VARCHAR(50), ToId)
FROM #IdTranslation
WHERE ConvertHistoricalJson = 1
ORDER BY FromId;

OPEN guid_cursor

FETCH NEXT FROM guid_cursor   
INTO @FromId, @ToId

WHILE @@FETCH_STATUS = 0  
BEGIN

    UPDATE hr
    SET hr.Results = REPLACE(hr.Results, @FromId, @ToId)
    FROM SeriesChartResults AS hr
    WHERE hr.Id IN (SELECT ToId
    FROM #IdTranslation)

    SET @runningCount = @runningCount + 1;
    IF(@runningCount % 10 = 0) BEGIN
        SET @ErrMsg = CONVERT(VARCHAR(10), @runningCount) + ' / ' + CONVERT(VARCHAR(10), @TotalCount)
        RAISERROR ( @ErrMsg, 0, 1) WITH NOWAIT
    END
    FETCH NEXT FROM guid_cursor
    INTO @FromId, @ToId
END
CLOSE guid_cursor;
DEALLOCATE guid_cursor;







PRINT N'Dropping Id Translation table'

DROP TABLE #idTranslation

PRINT N'Done'

