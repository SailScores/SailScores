-- Select * from Clubs

-- DECLARE @ClubName NVARCHAR(200);

-- SET @ClubName = 'SS Ägir2'


-- SET @ClubId = (
--         SELECT TOP 1 Id
--         FROM Clubs
--         WHERE Name = @ClubName
--         )

DECLARE @ClubId UNIQUEIDENTIFIER;

        SET @ClubId = 'ec25867e-248f-437c-9a93-f440770757f2'


PRINT N'Deleting Scores'

DELETE Scores
FROM Scores s
INNER JOIN Races r
    ON r.Id = s.RaceId
WHERE r.ClubId = @ClubId


PRINT N'Deleting Races'
DELETE
FROM races
FROM Races r
WHERE r.ClubId = @ClubId;

PRINT N'Deleting FleetBoatClass'
DELETE fbc
FROM FleetBoatClass AS fbc
INNER JOIN BoatClasses bc
    ON bc.Id = fbc.BoatClassId
WHERE bc.ClubId = @ClubId


PRINT N'Deleting BoatClass'
DELETE bc
FROM BoatClasses bc
INNER JOIN Competitors c
    ON c.BoatClassId = bc.Id
WHERE c.ClubId = @ClubId


PRINT N'Deleting CompetitorFleet'
DELETE cf
FROM CompetitorFleet AS cf
INNER JOIN Fleets AS f
    ON cf.FleetId = f.Id
WHERE f.ClubId = @ClubId


PRINT N'Deleting Competitors'
DELETE
FROM Competitors
WHERE ClubId = @ClubId


PRINT N'Deleting Fleets'
DELETE
FROM Fleets
WHERE ClubId = @ClubId


PRINT N'Deleting ScoreCodes'
DELETE
FROM ScoreCodes
WHERE ScoreCodes.ScoringSystemId in (Select Id from ScoringSystems where ScoringSystems.ClubId = @ClubId)


update Clubs set DefaultScoringSystemId = null where Id = @ClubId

PRINT N'Deleting ScoringSystems'
DELETE
FROM ScoringSystems
WHERE ClubId = @ClubId

PRINT N'Deleting SeriesRace'
DELETE
FROM SeriesRace
FROM SeriesRace
INNER JOIN Series
    ON Series.Id = SeriesRace.SeriesId
WHERE ClubId = @ClubId


PRINT N'Deleting HistoricalResults'
DELETE
FROM HistoricalResults
WHERE SeriesId IN (
        SELECT Id
        FROM Series
        WHERE ClubId = @ClubId
        )


PRINT N'Deleting Series'
DELETE
FROM Series
WHERE ClubId = @ClubId


PRINT N'Deleting Seasons'
DELETE
FROM Seasons
WHERE ClubId = @ClubId


PRINT N'Deleting UserPermissions'
DELETE
FROM UserPermissions
WHERE ClubId = @ClubId


PRINT N'Deleting Clubs'
DELETE
FROM Clubs
WHERE Id = @ClubId

