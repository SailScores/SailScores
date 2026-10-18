-- Scoring release: catch-up, run on prod immediately BEFORE the slot swap.
-- See FeaturePlans/Scoring-Release-Plan.md.
--
-- deploy.sql backfills data once, but the old code keeps running on prod until the swap.
-- This repeats those backfills for rows the old code added or changed since. Run it while
-- the old code is still live, right before swapping: after the swap, the new code saves a
-- blank discard pattern or participation on purpose (it means "inherit from the parent"),
-- and step 3 below would undo that.
--
-- Safe to run more than once. Prints how many rows each step changed.
--
-- Stage runs the new code on this database, so systems saved there during smoke tests may
-- be blank on purpose. Set @SmokeTestClubInitials to the test club to leave its systems alone.

SET XACT_ABORT ON;
SET NOCOUNT ON;

DECLARE @SmokeTestClubInitials NVARCHAR(50) = NULL;   -- e.g. N'TEST'
DECLARE @SmokeTestClubId UNIQUEIDENTIFIER = (
    SELECT [Id] FROM [Clubs] WHERE [Initials] = @SmokeTestClubInitials);

IF @SmokeTestClubInitials IS NOT NULL AND @SmokeTestClubId IS NULL
    THROW 50000, 'No club has the initials in @SmokeTestClubInitials.', 1;

BEGIN TRANSACTION;

-- 1. Summary series: SummaryRollupMode follows ChildrenSeriesAsSingleRace, which the old
--    code still sets (migration AddSummaryRollupModeAndScoringSystemClubAccess). The new
--    code keeps the two in step for modes 0 and 1, and mode 2 has no old equivalent.
UPDATE [Series]
SET [SummaryRollupMode] = CASE WHEN [ChildrenSeriesAsSingleRace] = 1 THEN 1 ELSE 0 END
WHERE [SummaryRollupMode] IN (0, 1)
    AND [SummaryRollupMode] <> CASE WHEN [ChildrenSeriesAsSingleRace] = 1 THEN 1 ELSE 0 END;
PRINT CONCAT('1. Summary series rollup mode updated: ', @@ROWCOUNT);

-- 2. Base systems the old code created have no calculator type. Same name rules as
--    migration AddScoringCalculatorType.
UPDATE [ScoringSystems] SET [CalculatorType] =
    CASE
        WHEN [Name] COLLATE Latin1_General_CS_AS LIKE '%High Point Percentage%' THEN 8
        WHEN [Name] COLLATE Latin1_General_CS_AS LIKE '%Cox-Sprague%' THEN 9
        WHEN [Name] COLLATE Latin1_General_CS_AS LIKE '%Low Point Average%'
            AND [Name] COLLATE Latin1_General_CS_AS LIKE '%without DNCs%' THEN 7
        WHEN [Name] COLLATE Latin1_General_CS_AS LIKE '%Low Point Average%' THEN 6
        WHEN [Name] COLLATE Latin1_General_CS_AS LIKE '%First = 0%' THEN 3
        WHEN [Name] COLLATE Latin1_General_CS_AS LIKE '%First = %'
            AND [Name] COLLATE Latin1_General_CS_AS LIKE '%.75%' THEN 5
        WHEN [Name] COLLATE Latin1_General_CS_AS LIKE '%First = %'
            AND [Name] COLLATE Latin1_General_CS_AS LIKE '%.7%' THEN 4
        WHEN [Name] COLLATE Latin1_General_CS_AS LIKE '%Top X High Point%' THEN 10
        WHEN [Name] COLLATE Latin1_General_CS_AS LIKE '%Appendix A%'
            AND [Name] COLLATE Latin1_General_CS_AS LIKE '%pre-2025%' THEN 2
        WHEN [Name] COLLATE Latin1_General_CS_AS LIKE 'PWA Standard%' THEN 11
        ELSE 1
    END
WHERE [ParentSystemId] IS NULL AND [CalculatorType] IS NULL;
PRINT CONCAT('2. Base systems given a calculator type: ', @@ROWCOUNT);

-- 3. Child systems with a blank discard pattern or participation meant "none" to the old
--    code; the new code inherits the parent's value instead. Write the old meaning
--    explicitly, as migration InheritDiscardPatternAndParticipation did.
WITH Chain AS (
    SELECT s.[Id] AS SystemId, s.[ParentSystemId] AS AncestorId, 1 AS Depth
    FROM [ScoringSystems] s
    WHERE s.[ParentSystemId] IS NOT NULL
    UNION ALL
    SELECT c.SystemId, a.[ParentSystemId], c.Depth + 1
    FROM Chain c
    JOIN [ScoringSystems] a ON a.[Id] = c.AncestorId
    WHERE a.[ParentSystemId] IS NOT NULL AND c.Depth < 20
)
UPDATE s SET [DiscardPattern] = '0'
FROM [ScoringSystems] s
WHERE NULLIF(LTRIM(RTRIM(s.[DiscardPattern])), '') IS NULL
    AND (@SmokeTestClubId IS NULL OR s.[ClubId] IS NULL OR s.[ClubId] <> @SmokeTestClubId)
    AND EXISTS (
        SELECT 1 FROM Chain c
        JOIN [ScoringSystems] a ON a.[Id] = c.AncestorId
        WHERE c.SystemId = s.[Id]
            AND NULLIF(LTRIM(RTRIM(a.[DiscardPattern])), '') IS NOT NULL);
PRINT CONCAT('3a. Child systems given discard pattern 0: ', @@ROWCOUNT);

WITH Chain AS (
    SELECT s.[Id] AS SystemId, s.[ParentSystemId] AS AncestorId, 1 AS Depth
    FROM [ScoringSystems] s
    WHERE s.[ParentSystemId] IS NOT NULL
    UNION ALL
    SELECT c.SystemId, a.[ParentSystemId], c.Depth + 1
    FROM Chain c
    JOIN [ScoringSystems] a ON a.[Id] = c.AncestorId
    WHERE a.[ParentSystemId] IS NOT NULL AND c.Depth < 20
)
UPDATE s SET [ParticipationPercent] =
    -- Top X High Point treated a blank as "count 5 races".
    CASE WHEN EXISTS (
            SELECT 1 FROM Chain c
            JOIN [ScoringSystems] a ON a.[Id] = c.AncestorId
            WHERE c.SystemId = s.[Id] AND a.[ParentSystemId] IS NULL AND a.[CalculatorType] = 10)
        THEN 5 ELSE 0 END
FROM [ScoringSystems] s
WHERE s.[ParticipationPercent] IS NULL
    AND (@SmokeTestClubId IS NULL OR s.[ClubId] IS NULL OR s.[ClubId] <> @SmokeTestClubId)
    AND EXISTS (
        SELECT 1 FROM Chain c
        JOIN [ScoringSystems] a ON a.[Id] = c.AncestorId
        WHERE c.SystemId = s.[Id] AND a.[ParticipationPercent] IS NOT NULL);
PRINT CONCAT('3b. Child systems given their old participation: ', @@ROWCOUNT);

COMMIT;
