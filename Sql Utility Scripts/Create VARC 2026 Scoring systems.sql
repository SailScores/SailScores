-- Creates the sitewide VARC 2026 base scoring systems.
-- Score codes are copied from the sitewide "Top X High Point" base system
-- (CalculatorType 10). Safe to run more than once: existing VARC 2026 base systems are skipped.
--
-- VARC 2026 Shorthanded (CalculatorType 15): Top X High Point race scores; each boat's
--   best 3 scores (ScoresToCount) make up the total. Ties: A8.1, then A8.2.
-- VARC 2026 (CalculatorType 16): the same, but a race counts only when at least 2 boats
--   (MinimumCompetitors) came to the start. For series where ties stand after A8.1,
--   create a club child system with "Ties stand after A8.1" set to Yes.
--
-- The club's answers (2026-10-22) change some copied codes: BFD and DNE score and discard
-- like a DNF, and the penalties SCP, DPI and ZFP score as DNC (0, not a boat racing). These
-- updates also apply to VARC 2026 base systems that already exist.
--
-- The new systems start restricted: only the club named in @AccessClubInitials can use them.
-- Set it before running (for example the test club). To open the systems to more clubs
-- later, add them in Site Admin > Shared Scoring Systems.

SET XACT_ABORT ON;

DECLARE @AccessClubInitials NVARCHAR(50) = N'SET-ME';  -- the one club allowed to use the new systems

DECLARE @AccessClubId UNIQUEIDENTIFIER = (
    SELECT [Id] FROM [Clubs] WHERE [Initials] = @AccessClubInitials);

IF @AccessClubId IS NULL
    THROW 50000, 'Set @AccessClubInitials to the initials of an existing club.', 1;

BEGIN TRANSACTION;

DECLARE @SourceSystemId UNIQUEIDENTIFIER = (
    SELECT TOP 1 [Id] FROM [ScoringSystems]
    WHERE [ClubId] IS NULL AND [ParentSystemId] IS NULL AND [CalculatorType] = 10
    ORDER BY [Name]);

IF @SourceSystemId IS NULL
    THROW 50000, 'No sitewide "Top X High Point" base system (CalculatorType 10) found.', 1;

DECLARE @Systems TABLE (
    [Id] UNIQUEIDENTIFIER,
    [Name] NVARCHAR(100),
    [CalculatorType] INT,
    [Settings] NVARCHAR(MAX));

INSERT INTO @Systems VALUES
    ('3c9e5a71-2f4d-4b8e-a6c1-7d0e9b2f5a38', 'VARC 2026 Shorthanded', 15, '{"ScoresToCount":3}'),
    ('8b2d6f14-9e3a-4c75-b0d8-5a1f7e3c9264', 'VARC 2026', 16, '{"ScoresToCount":3,"MinimumCompetitors":2}');

DELETE s FROM @Systems s
WHERE EXISTS (
    SELECT 1 FROM [ScoringSystems] ss
    WHERE ss.[ClubId] IS NULL AND ss.[ParentSystemId] IS NULL AND ss.[CalculatorType] = s.[CalculatorType]);

INSERT INTO [ScoringSystems]
    ([Id], [ClubId], [Name], [DiscardPattern], [ParentSystemId], [ParticipationPercent],
     [IsSiteDefault], [CalculatorType], [DiscardMethod], [Settings], [IsRestrictedToClubs])
SELECT [Id], NULL, [Name], '0', NULL, NULL, 0, [CalculatorType], 0, [Settings], 1
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

-- The club's answers, on the new or existing VARC 2026 base systems.
DECLARE @VarcSystemIds TABLE ([Id] UNIQUEIDENTIFIER);
INSERT INTO @VarcSystemIds
SELECT [Id] FROM [ScoringSystems]
WHERE [ClubId] IS NULL AND [ParentSystemId] IS NULL AND [CalculatorType] IN (15, 16);

-- Codes the NOR doesn't name count the same as a DNF.
UPDATE [ScoreCodes]
SET [Discardable] = 1
WHERE [ScoringSystemId] IN (SELECT [Id] FROM @VarcSystemIds) AND [Name] IN ('BFD', 'DNE');

-- Penalties score as DNC: 0, and the boat isn't racing or scored.
UPDATE [ScoreCodes]
SET [Description] = [Description] + ': scored as DNC',
    [Formula] = 'FIX', [FormulaValue] = 0, [FormulaValue_Decimal] = 0,
    [PreserveResult] = 0, [AdjustOtherScores] = 1,
    [Started] = 0, [CameToStart] = 0, [Finished] = 0
WHERE [ScoringSystemId] IN (SELECT [Id] FROM @VarcSystemIds) AND [Name] IN ('SCP', 'DPI', 'ZFP')
    AND [Description] NOT LIKE '%: scored as DNC';

COMMIT;
