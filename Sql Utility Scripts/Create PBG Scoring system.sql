-- Creates the sitewide PBG base scoring system (CalculatorType 14).
-- Score codes are copied from the sitewide "Low Point Average without DNCs" base system
-- (CalculatorType 7). Safe to run more than once: skipped when it exists.
--
-- Low point average without DNCs; 50% participation to rank, discarded races included.
-- A summary series using its child series' races keeps their discards; any other series
-- (a daily series) uses the discard pattern: a boat that sailed 6 or more races discards
-- its worst (0,0,0,0,0,1, inherited from this base system unless the club's system sets
-- one). Rules: docs/ScoringSystems/Pbg.md.
--
-- The new system starts restricted: only the club named in @AccessClubInitials can use it.
-- Set it before running (for example the test club). To open the system to more clubs
-- later, add them in Site Admin > Shared Scoring Systems.

SET XACT_ABORT ON;

DECLARE @AccessClubInitials NVARCHAR(50) = N'SET-ME';  -- the one club allowed to use the new system

DECLARE @AccessClubId UNIQUEIDENTIFIER = (
    SELECT [Id] FROM [Clubs] WHERE [Initials] = @AccessClubInitials);

IF @AccessClubId IS NULL
    THROW 50000, 'Set @AccessClubInitials to the initials of an existing club.', 1;

BEGIN TRANSACTION;

DECLARE @SourceSystemId UNIQUEIDENTIFIER = (
    SELECT TOP 1 [Id] FROM [ScoringSystems]
    WHERE [ClubId] IS NULL AND [ParentSystemId] IS NULL AND [CalculatorType] = 7
    ORDER BY [Name]);

IF @SourceSystemId IS NULL
    THROW 50000, 'No sitewide "Low Point Average without DNCs" base system (CalculatorType 7) found.', 1;

DECLARE @Systems TABLE (
    [Id] UNIQUEIDENTIFIER,
    [Name] NVARCHAR(100),
    [DiscardPattern] NVARCHAR(MAX),
    [CalculatorType] INT,
    [DiscardMethod] INT,
    [Settings] NVARCHAR(MAX));

INSERT INTO @Systems VALUES
    ('b7f3c2d4-6a1e-4f8b-9c05-3e2d8a71f4b6', 'PBG', '0,0,0,0,0,1', 14, 4, NULL);

DELETE s FROM @Systems s
WHERE EXISTS (
    SELECT 1 FROM [ScoringSystems] ss
    WHERE ss.[ClubId] IS NULL AND ss.[ParentSystemId] IS NULL AND ss.[CalculatorType] = s.[CalculatorType]);

INSERT INTO [ScoringSystems]
    ([Id], [ClubId], [Name], [DiscardPattern], [ParentSystemId], [ParticipationPercent],
     [IsSiteDefault], [CalculatorType], [DiscardMethod], [Settings], [IsRestrictedToClubs])
SELECT [Id], NULL, [Name], [DiscardPattern], NULL, 50, 0, [CalculatorType], [DiscardMethod], [Settings], 1
FROM @Systems;

INSERT INTO [ScoringSystemClubAccess] ([ScoringSystemId], [ClubId])
SELECT [Id], @AccessClubId FROM @Systems;

INSERT INTO [ScoreCodes]
    ([Id], [Name], [Description], [PreserveResult], [Discardable], [Started], [FormulaValue],
     [FormulaValue_Decimal], [AdjustOtherScores], [CameToStart], [Finished], [Formula], [ScoreLike],
     [CountAsParticipation], [IncludeInAverage], [ScoringSystemId])
SELECT NEWID(), sc.[Name], sc.[Description], sc.[PreserveResult], sc.[Discardable], sc.[Started],
    sc.[FormulaValue], sc.[FormulaValue_Decimal], sc.[AdjustOtherScores], sc.[CameToStart],
    sc.[Finished], sc.[Formula], sc.[ScoreLike], sc.[CountAsParticipation], sc.[IncludeInAverage], s.[Id]
FROM [ScoreCodes] sc
CROSS JOIN @Systems s
WHERE sc.[ScoringSystemId] = @SourceSystemId;

COMMIT;
