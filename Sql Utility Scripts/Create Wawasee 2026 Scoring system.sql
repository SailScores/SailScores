-- Creates the sitewide Wawasee 2026 base scoring system (CalculatorType 17) for
-- Wawasee Yacht Club's seasonal fleet series. Safe to run more than once: skipped when it exists.
--
-- 51% of races competed in (came to the start; a DNS doesn't count) to rank; 70% sailed (excused absences
-- count) to avoid penalties; a day's races count only when 4 boats started a race that day.
-- Each fleet's child system sets its penalty points (last year's boats qualifying for
-- awards, plus one). Rules: docs/ScoringSystems/Wawasee2026.md.
--
-- Score codes are copied from the sitewide Appendix A base system (CalculatorType 1),
-- except DNC: the club uses DNP (did not participate) instead, and the system scores a
-- missing result as DNP. DNP scores the boat's average finish; its Formula Value of 1 keeps
-- the cap on every score at the boats in the series + 1. DNS is scored like DNP: the average
-- finish, and it doesn't count toward 51% or 70%. DNF, RET and DSQ score the boats that
-- started, plus one (STR+). Three codes are added:
--   EXC  excused absence: average finish; counts toward 70%, not 51%.
--   BKD  breakdown throw-out: average finish; counts toward both. One per series: a score
--        code group here scores the second and later as RET.
--   TLE  outside the time limit (SI 15.1): the boats that finished, plus two.
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
DECLARE @BreakdownGroupId UNIQUEIDENTIFIER = '33adeffe-254e-41c3-b788-1991995cb884';

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
        AND sc.[Name] NOT IN ('DNC', 'DNP', 'EXC', 'BKD', 'TLE');

    -- The club's answer: DNF, RET and DSQ score the starters + 1.
    UPDATE [ScoreCodes]
    SET [Formula] = 'STR+', [FormulaValue] = 1, [FormulaValue_Decimal] = 1
    WHERE [ScoringSystemId] = @SystemId AND [Name] IN ('DNF', 'RET', 'DSQ');

    -- The club's answer: a DNS is a DNP.
    UPDATE [ScoreCodes]
    SET [Description] = 'Did not start: scored the average finish, like DNP',
        [Formula] = 'AVE', [FormulaValue] = 1, [FormulaValue_Decimal] = 1,
        [Started] = 0, [CameToStart] = 0, [AdjustOtherScores] = 0, [CountAsParticipation] = 0
    WHERE [ScoringSystemId] = @SystemId AND [Name] = 'DNS';

    INSERT INTO [ScoreCodes]
        ([Id], [Name], [Description], [PreserveResult], [Discardable], [Started], [FormulaValue],
         [FormulaValue_Decimal], [AdjustOtherScores], [CameToStart], [Finished], [Formula], [ScoreLike],
         [CountAsParticipation], [IncludeInAverage], [ScoringSystemId])
    VALUES
        (NEWID(), 'DNP', 'Did not participate: scored the average finish',
         0, 1, 0, 1, 1, 0, 0, 0, 'AVE', NULL, 0, NULL, @SystemId),
        (NEWID(), 'EXC', 'Excused absence (committee boat duty or off-the-lake regatta): scored the average finish',
         0, 1, 0, NULL, NULL, 0, 0, 0, 'AVE', NULL, 1, NULL, @SystemId),
        (NEWID(), 'BKD', 'Breakdown throw-out: scored the average finish',
         0, 1, 1, NULL, NULL, 0, 1, 0, 'AVE', NULL, 1, NULL, @SystemId),
        (NEWID(), 'TLE', 'Did not finish within the time limit: boats that finished, plus two',
         0, 1, 1, 2, 2, 1, 1, 0, 'FIN+', NULL, NULL, NULL, @SystemId);

    -- One breakdown throw-out per series: later ones score as RET.
    INSERT INTO [ScoreCodeGroups]
        ([Id], [ScoringSystemId], [Name], [LimitationType], [LimitationValue],
         [UseNonDiscardedRaces], [OverageCodeName], [OverageSelectionMethod])
    VALUES
        (@BreakdownGroupId, @SystemId, 'Breakdown throw-outs', 0, 1, 0, 'RET', 0);

    INSERT INTO [ScoreCodeGroupCodes] ([ScoreCodeGroupId], [CodeName])
    VALUES (@BreakdownGroupId, 'BKD');
END

COMMIT;
