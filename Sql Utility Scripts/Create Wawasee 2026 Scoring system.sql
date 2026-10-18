-- Creates the sitewide Wawasee 2026 base scoring system (CalculatorType 17) for
-- Wawasee Yacht Club's seasonal fleet series. Safe to run more than once: skipped when it exists.
--
-- 51% of races started to rank; 70% sailed (excused absences count) to avoid penalties;
-- a day's races count only when 4 boats started a race that day. Each fleet's child system
-- sets its penalty points (last year's boats qualifying for awards, plus one).
--
-- Score codes are copied from the sitewide Appendix A base system (CalculatorType 1),
-- except DNC, which scores the boat's average finish here. Two codes are added:
--   EXC  excused absence: average finish; counts toward 70%, not 51%.
--   BKD  breakdown throw-out: average finish; counts toward both. Limit it to one per
--        series with a score code group on the club's system.
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

DECLARE @SystemId UNIQUEIDENTIFIER = 'e41a7c93-5d26-4f0b-8b3e-92c6d1a85f07';

DECLARE @SourceSystemId UNIQUEIDENTIFIER = (
    SELECT TOP 1 [Id] FROM [ScoringSystems]
    WHERE [ClubId] IS NULL AND [ParentSystemId] IS NULL AND [CalculatorType] = 1
    ORDER BY [Name]);

IF @SourceSystemId IS NULL
    THROW 50000, 'No sitewide Appendix A base system (CalculatorType 1) found.', 1;

IF NOT EXISTS (
    SELECT 1 FROM [ScoringSystems]
    WHERE [ClubId] IS NULL AND [ParentSystemId] IS NULL AND [CalculatorType] = 17)
BEGIN
    INSERT INTO [ScoringSystems]
        ([Id], [ClubId], [Name], [DiscardPattern], [ParentSystemId], [ParticipationPercent],
         [IsSiteDefault], [CalculatorType], [DiscardMethod], [Settings], [IsRestrictedToClubs])
    VALUES
        (@SystemId, NULL, 'Wawasee 2026', '0', NULL, 51, 0, 17, 0,
         '{"PenaltyThresholdPercent":70,"MinimumCompetitors":4}', 1);

    INSERT INTO [ScoringSystemClubAccess] ([ScoringSystemId], [ClubId])
    VALUES (@SystemId, @AccessClubId);

    INSERT INTO [ScoreCodes]
        ([Id], [Name], [Description], [PreserveResult], [Discardable], [Started], [FormulaValue],
         [FormulaValue_Decimal], [AdjustOtherScores], [CameToStart], [Finished], [Formula], [ScoreLike],
         [CountAsParticipation], [IncludeInAverage], [ScoringSystemId])
    SELECT NEWID(), sc.[Name], sc.[Description], sc.[PreserveResult], sc.[Discardable], sc.[Started],
        sc.[FormulaValue], sc.[FormulaValue_Decimal], sc.[AdjustOtherScores], sc.[CameToStart],
        sc.[Finished], sc.[Formula], sc.[ScoreLike], sc.[CountAsParticipation], sc.[IncludeInAverage], @SystemId
    FROM [ScoreCodes] sc
    WHERE sc.[ScoringSystemId] = @SourceSystemId
        AND sc.[Name] NOT IN ('DNC', 'EXC', 'BKD');

    INSERT INTO [ScoreCodes]
        ([Id], [Name], [Description], [PreserveResult], [Discardable], [Started], [FormulaValue],
         [FormulaValue_Decimal], [AdjustOtherScores], [CameToStart], [Finished], [Formula], [ScoreLike],
         [CountAsParticipation], [IncludeInAverage], [ScoringSystemId])
    VALUES
        (NEWID(), 'DNC', 'Did not come to the starting area: scored the average finish',
         0, 1, 0, NULL, NULL, 0, 0, 0, 'AVE', NULL, 0, NULL, @SystemId),
        (NEWID(), 'EXC', 'Excused absence (committee boat duty or off-the-lake regatta): scored the average finish',
         0, 1, 0, NULL, NULL, 0, 0, 0, 'AVE', NULL, 1, NULL, @SystemId),
        (NEWID(), 'BKD', 'Breakdown throw-out: scored the average finish',
         0, 1, 1, NULL, NULL, 0, 1, 0, 'AVE', NULL, 1, NULL, @SystemId);
END

COMMIT;
