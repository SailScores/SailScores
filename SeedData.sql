
Use Sailscores5;

Insert into Clubs
(
    Id,
    Name,
    Initials,
    Description,
    IsHidden
) VALUES
(
    '27002B14-6A05-DB49-9A7D-444647D0F6E7',
    'Test Yacht Club One',
    'TYC1',
    'For testing purposes. Any similarity to real clubs is accidental.',
    0
),
(
    '3EB70E69-AC39-2747-99C0-8305F3E43234',
    'Test Yacht Club Two',
    'TYC2',
    'For testing purposes. Club Number 2',
    0
),
(
    'D10FE7D3-183E-2541-84DB-974710B19E68',
    'Test Yacht Club Invisible',
    'TYCI',
    'For testing purposes. Invisible Club',
    0
)

Declare @ClubId1 UNIQUEIDENTIFIER;
Declare @ClubId2 UNIQUEIDENTIFIER;
Declare @ClubId3 UNIQUEIDENTIFIER;
SET @ClubId1 = (SELECT Id from Clubs where Initials = 'TYC1')
SET @ClubId2 = (SELECT Id from Clubs where Initials = 'TYC2')
SET @ClubId3 = (SELECT Id from Clubs where Initials = 'TYCI')


Insert into BoatClass
(
    Id, ClubId, Name, [Description]
) VALUES
(
    'BCF1843A-E19E-6042-A7C8-4F606125E83A',
    @ClubId1,
    'MC Scow',
    'Melges MC Scows'
),
(
    'E2C93B56-022E-C243-8805-D5F5C19FA626',
    @ClubId1,
    'M Scow',
    'M 16 Scows'
),(
    '2A8EFB7F-5B63-7342-A14F-B206D0DC1062',
    @ClubId1,
    '420',
    'High School 420s'
),(
    '3368AE15-B629-9E41-878D-17C016E47CB9',
    @ClubId2,
    'MC Scow',
    'Melges MC Scows'
),(
    '3A7FD3C1-9C77-0741-B80C-F74024BB97BC',
    @ClubId3,
    'J22',
    'J Boats'
)

-- Seasons

Insert into Seasons
( Id, ClubId, [Name], [Start], [End])
VALUES
(
    'D29E1F7F-6ED6-3A4B-ADC4-1697B96F4E6A',
    @ClubId1,
    '2016',
    '20160101',
    '20161231'
),(
    'C41827EE-217E-594F-94A1-86D5588B177C',
    @ClubId1,
    '2017',
    '20170101',
    '20171231'
),(
    '27002B14-6A05-DB49-9A7D-444647D0F6E7',
    @ClubId1,
    '2018',
    '20180101',
    '20181231'
),(
    '3EB70E69-AC39-2747-99C0-8305F3E43234',
    @ClubId1,
    '2019',
    '20190101',
    '20191231'
),(
    'E2FFA7D2-6B52-EC4B-AB86-A6924B9BA7B4',
    @ClubId2,
    '2018',
    '20180101',
    '20181231'
),(
    'BAE38FE0-85F6-5446-AE81-3E4D2CC06F9F',
    @ClubId3,
    '2018',
    '20180101',
    '20181231'
)

-- Series
Insert into Series
(
    Id, ClubId, Name, [Description], SeasonId
)
VALUES
( 'D10FE7D3-183E-2541-84DB-974710B19E68' , @ClubId1, 'All 2018 Races', 'Every race for 2018', (Select Id from Seasons where Name = '2018' and ClubId = @ClubId1)),
( 'BCF1843A-E19E-6042-A7C8-4F606125E83A' , @ClubId1, 'Some 2018 Races', 'The first few races of 2018', (Select Id from Seasons where Name = '2018' and ClubId = @ClubId1)),
( 'E2C93B56-022E-C243-8805-D5F5C19FA626' , @ClubId1, 'Last 2018 Race', 'A single race', (Select Id from Seasons where Name = '2018' and ClubId = @ClubId1)),
( '2A8EFB7F-5B63-7342-A14F-B206D0DC1062' , @ClubId1, '2018 Light Wind', 'Not many light wind races', (Select Id from Seasons where Name = '2018' and ClubId = @ClubId1)),
( '82F3D58B-34BC-C546-88F1-01329AC18AB8' , @ClubId2, '2018 Races', 'Some of the races we want to score in the series', (Select Id from Seasons where Name = '2018' and ClubId = @ClubId2)),
( 'C58A957D-A456-5D45-AC3F-CC05B7D0FB86' , @ClubId3, '2018 Races', 'A few races', (Select Id from Seasons where Name = '2018' and ClubId = @ClubId3))

-- Fleets
Insert into Fleets
( Id, ClubId, Name, [Description], FleetType)
VALUES
( 'E799427B-9C54-DF4F-A65C-099B05EF929E', @ClubId1, 'MC Scows', null, 2),
( '18F7E2BE-E16C-674F-9056-1D6543A90BC7', @ClubId1, 'M Scows', null, 2),
( '69D356CC-AE6D-FB45-BD29-1A1CCF07EAC5', @ClubId1, 'Top MCs', null, 3),
( '38A03604-D23C-AC4F-9ABE-1CD744C26DA3', @ClubId1, 'Everyone', null, 1),
( '23C7C633-E9E7-F047-806C-21EAB17CB42E', @ClubId2, 'Everyone', null, 1),
( '1797A130-6C92-9649-9812-05A4C35C345C', @ClubId3, 'Everyone', null, 1)

-- FleetBoatClass
Insert into FleetBoatClass
( BoatClassId, FleetId)
Values
('BCF1843A-E19E-6042-A7C8-4F606125E83A', 'E799427B-9C54-DF4F-A65C-099B05EF929E'),
( 'E2C93B56-022E-C243-8805-D5F5C19FA626' , '18F7E2BE-E16C-674F-9056-1D6543A90BC7')

-- Competitors
Insert into Competitors
( Id, ClubId, Name, SailNumber, AlternativeSailNumber, BoatName, Notes, BoatClassId)
VALUES
( '2A6C5585-21A9-9B4F-B380-1BA7D50F9FD9', @ClubId1, 'Alice Sailor' , '2101', null, 'FastBoat', null,  'BCF1843A-E19E-6042-A7C8-4F606125E83A'),
( '627A7F6B-FF98-854E-9DFC-07767D7949F4', @ClubId1, 'Bob Sailor' , 'MC10', null, '', null,  'BCF1843A-E19E-6042-A7C8-4F606125E83A'),
( '40072044-6186-8043-8B6B-549DB108C521', @ClubId1, 'Charlie Sailor' , '1803', null, 'Faster Than Bob', null,  'BCF1843A-E19E-6042-A7C8-4F606125E83A'),
( 'D38EE07E-1952-4D42-98CD-279E7E3EA3FD', @ClubId1, 'David Super Sailor' , '2901', null, 'UltraMarine', null,  'BCF1843A-E19E-6042-A7C8-4F606125E83A'),
( 'C4AC71C1-CDA7-AC44-B460-74CF0ACBFF33', @ClubId1, 'Earnest Starting Out' , 'My123', null, '- various -', null,  'BCF1843A-E19E-6042-A7C8-4F606125E83A'),
( '90F94761-D647-1D4D-A2D5-7FEDD6A92758', @ClubId1, 'Frank Em' , 'TC12', null, 'M & Em', null,  'E2C93B56-022E-C243-8805-D5F5C19FA626'),
( '04DBEE24-D762-9549-A51C-4BFB068F6457', @ClubId1, 'George McFly' , 'TC000', null, 'Multi', null,  'E2C93B56-022E-C243-8805-D5F5C19FA626'),
( 'B28679D2-D074-BC4C-8B45-CECE79F39EAD', @ClubId2, 'Harriet Hellion McFly' , '1234', null, 'Multi', null,  '3368AE15-B629-9E41-878D-17C016E47CB9'),
( 'F967DCF9-6F5A-C64C-9C13-0A897D5A7E66', @ClubId2, 'Isabel Isaak' , '4321', null, 'Multi', null,  '3368AE15-B629-9E41-878D-17C016E47CB9'),
( 'A87BFE85-DF0E-EC45-9B21-BD3EF798A48C', @ClubId3, 'Jimmy John James III' , '5515', null, 'Multi', null,  '3A7FD3C1-9C77-0741-B80C-F74024BB97BC'),
( '0971D564-13E8-A347-9221-A7CA9B4D1473', @ClubId3, 'Kelly Kickeroo' , '432', null, 'Multi', null,  '3A7FD3C1-9C77-0741-B80C-F74024BB97BC')


-- CompetitorFleets
Insert into CompetitorFleet
(CompetitorId, FleetId)
VALUES
( '40072044-6186-8043-8B6B-549DB108C521', '69D356CC-AE6D-FB45-BD29-1A1CCF07EAC5'),
( 'D38EE07E-1952-4D42-98CD-279E7E3EA3FD', '69D356CC-AE6D-FB45-BD29-1A1CCF07EAC5')

-- ScoreCodes

Insert into ScoreCodes
( Id, ClubId, Text, [Description], CountAsCompetitor, Discardable, UseAverageResult, CompetitorCountPlus)
VALUES
( 'F4E6DA35-4D8F-B546-8288-47F5418AC553', @ClubId1, 'DNF', 'Did not finish', 1, 1, 0, 1),
( 'AEEA20CA-4FBB-8F48-B437-CA8DAFA41880', @ClubId1, 'DNS', 'Did not start', 0, 1, 0, 1),
( '780F6D2F-3E2C-2C4D-AE5E-3B21074E34C2', @ClubId1, 'DNC', 'Did not compete', 0, 1, 0, 2),
( 'ED216C3B-AA65-8246-9007-2E0DC2505C14', @ClubId1, 'SB', 'Did not compete', 0, 1, 1, null),
( '90F94761-D647-1D4D-A2D5-7FEDD6A92758', @ClubId1, 'BAD', 'Non discardable', 0, 0, 0, 2)




Declare @ClubId1 UNIQUEIDENTIFIER;
Declare @ClubId2 UNIQUEIDENTIFIER;
Declare @ClubId3 UNIQUEIDENTIFIER;
SET @ClubId1 = (SELECT Id from Clubs where Initials = 'TYC1')
SET @ClubId2 = (SELECT Id from Clubs where Initials = 'TYC2')
SET @ClubId3 = (SELECT Id from Clubs where Initials = 'TYCI')


-- Races
Insert into Races
( Id, ClubId, Name, Date, [Order], [Description], FleetId)
VALUES
(  'D1C25E25-6493-244C-A1A9-4E33C61F3260', @ClubId1, 'First MC race of the year', '20180401', 1, null, 'E799427B-9C54-DF4F-A65C-099B05EF929E' ),
(  'F9E205BB-9D6D-2A44-AAEE-0F1DB4686F01', @ClubId1, null, '20180401', 2, null, 'E799427B-9C54-DF4F-A65C-099B05EF929E' ),
(  'C96C2AC5-C3D8-484B-8E9B-026026BD5287', @ClubId1, null, '20180402', 3, null, 'E799427B-9C54-DF4F-A65C-099B05EF929E' ),
(  'F80828F4-BB8F-5746-8837-F2079E1C3A25', @ClubId1, null, '20180410', 4, null, 'E799427B-9C54-DF4F-A65C-099B05EF929E' ),
(  '9B262DFC-094D-E447-B627-DE8821F161ED', @ClubId1, 'First M16 race of the year', '20180401', 1, null, '18F7E2BE-E16C-674F-9056-1D6543A90BC7' ),
(  'BCDBAD7C-4E55-D54D-B80F-0DB843009B8A', @ClubId1, null, '20180401', 2, null, '18F7E2BE-E16C-674F-9056-1D6543A90BC7' ),
(  'B6642743-F28A-1343-9594-8521014D8A7C', @ClubId1, null, '20180402', 3, null, '18F7E2BE-E16C-674F-9056-1D6543A90BC7' ),
(  'F1E90BB8-2D98-DE4F-86A4-8FCD31721B13', @ClubId1, null, '20180410', 4, null, '18F7E2BE-E16C-674F-9056-1D6543A90BC7' ),
(  'F000DA89-4F6C-494B-9426-CF74A8EB430F', @ClubId2, 'Club 2 had a race', '20180410', 4, null, '23C7C633-E9E7-F047-806C-21EAB17CB42E' ),
(  '708B46FC-B1AA-8E44-BF86-DA0320DC8130', @ClubId3, null, '20180410', 4, null, '1797A130-6C92-9649-9812-05A4C35C345C' ),
(  'C83D0994-B686-A543-B836-4196DE6EE796', @ClubId3, null, '20180410', 4, null, '1797A130-6C92-9649-9812-05A4C35C345C' )


-- SeriesRaces
insert into SeriesRaces
(RaceId, SeriesId)
VALUES
('F9E205BB-9D6D-2A44-AAEE-0F1DB4686F01' , 'D10FE7D3-183E-2541-84DB-974710B19E68'),
('C96C2AC5-C3D8-484B-8E9B-026026BD5287' , 'D10FE7D3-183E-2541-84DB-974710B19E68'),
('F80828F4-BB8F-5746-8837-F2079E1C3A25' , 'D10FE7D3-183E-2541-84DB-974710B19E68'),
('D1C25E25-6493-244C-A1A9-4E33C61F3260' , 'D10FE7D3-183E-2541-84DB-974710B19E68'),
('9B262DFC-094D-E447-B627-DE8821F161ED' , 'D10FE7D3-183E-2541-84DB-974710B19E68'),
('BCDBAD7C-4E55-D54D-B80F-0DB843009B8A' , 'D10FE7D3-183E-2541-84DB-974710B19E68'),
('B6642743-F28A-1343-9594-8521014D8A7C' , 'D10FE7D3-183E-2541-84DB-974710B19E68'),
('F1E90BB8-2D98-DE4F-86A4-8FCD31721B13' , 'D10FE7D3-183E-2541-84DB-974710B19E68'),
('F000DA89-4F6C-494B-9426-CF74A8EB430F' , '82F3D58B-34BC-C546-88F1-01329AC18AB8'),
('708B46FC-B1AA-8E44-BF86-DA0320DC8130' , 'C58A957D-A456-5D45-AC3F-CC05B7D0FB86'),
('C83D0994-B686-A543-B836-4196DE6EE796' , 'C58A957D-A456-5D45-AC3F-CC05B7D0FB86'),
('F9E205BB-9D6D-2A44-AAEE-0F1DB4686F01' , 'BCF1843A-E19E-6042-A7C8-4F606125E83A'),
('C96C2AC5-C3D8-484B-8E9B-026026BD5287' , 'BCF1843A-E19E-6042-A7C8-4F606125E83A'),
('F80828F4-BB8F-5746-8837-F2079E1C3A25' , 'E2C93B56-022E-C243-8805-D5F5C19FA626')



-- Scores

Insert into Scores
(Id, CompetitorId, RaceId, Place, Code)
VALUES

( '8811EC3C-D4F6-3743-B579-2B6620DDDCB5' , '2A6C5585-21A9-9B4F-B380-1BA7D50F9FD9' ,  'D1C25E25-6493-244C-A1A9-4E33C61F3260', 1, null ),
( 'F9A7602D-DF99-044C-BFB2-D0DEC2809922' , '627A7F6B-FF98-854E-9DFC-07767D7949F4' ,  'D1C25E25-6493-244C-A1A9-4E33C61F3260', 2, null ),
( '3A3CCEEC-2A88-6743-9749-C09A1CA9FF3C' , '40072044-6186-8043-8B6B-549DB108C521' ,  'D1C25E25-6493-244C-A1A9-4E33C61F3260', 3, null ),
( '42F6B22A-21B3-3F4D-B8FF-34F1542393CE' , 'D38EE07E-1952-4D42-98CD-279E7E3EA3FD' ,  'D1C25E25-6493-244C-A1A9-4E33C61F3260', 4, null ),
( 'AA6582E2-4CC8-DB4A-B36B-1E749559B72F' , 'C4AC71C1-CDA7-AC44-B460-74CF0ACBFF33' ,  'D1C25E25-6493-244C-A1A9-4E33C61F3260', 5, null ),

( '986782F1-ED74-DF45-9E4B-954E2E7E5F78' , '2A6C5585-21A9-9B4F-B380-1BA7D50F9FD9' ,  'F9E205BB-9D6D-2A44-AAEE-0F1DB4686F01', 1, null ),
( '2B65686B-2FB2-6E44-ADF1-F59027548DB4' , '627A7F6B-FF98-854E-9DFC-07767D7949F4' ,  'F9E205BB-9D6D-2A44-AAEE-0F1DB4686F01', 2, null ),
( '63C82A1A-9BF8-3842-8802-9098076D6D73' , '40072044-6186-8043-8B6B-549DB108C521' ,  'F9E205BB-9D6D-2A44-AAEE-0F1DB4686F01', null, 'DNC' ),
( 'ADCF0167-646D-9046-9618-01383E86B55B' , 'D38EE07E-1952-4D42-98CD-279E7E3EA3FD' ,  'F9E205BB-9D6D-2A44-AAEE-0F1DB4686F01', 3, null ),
( '5FB30CE7-63C3-5D46-8A38-207B48AAFE99' , 'C4AC71C1-CDA7-AC44-B460-74CF0ACBFF33' ,  'F9E205BB-9D6D-2A44-AAEE-0F1DB4686F01', 4, null )
















