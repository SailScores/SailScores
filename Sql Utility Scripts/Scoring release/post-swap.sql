-- Scoring release: run on prod right AFTER the slot swap.
-- See FeaturePlans/Scoring-Release-Plan.md.
--
-- 1. Repeats the one catch-up that is safe once the new code is live (summary series
--    rollup mode), for anything the old code changed between pre-swap.sql and the swap.
-- 2. Lists rows to review by hand. It changes nothing for these: once the new code is
--    live, a blank discard pattern or participation can be deliberate ("inherit").
--
-- Safe to run more than once.

SET XACT_ABORT ON;
SET NOCOUNT ON;

BEGIN TRANSACTION;

UPDATE [Series]
SET [SummaryRollupMode] = CASE WHEN [ChildrenSeriesAsSingleRace] = 1 THEN 1 ELSE 0 END
WHERE [SummaryRollupMode] IN (0, 1)
    AND [SummaryRollupMode] <> CASE WHEN [ChildrenSeriesAsSingleRace] = 1 THEN 1 ELSE 0 END;
PRINT CONCAT('1. Summary series rollup mode updated: ', @@ROWCOUNT);

COMMIT;

-- 2a. Base systems with no calculator type: scored as Appendix A. Expected: none.
SELECT [Id], [Name], [ClubId]
FROM [ScoringSystems]
WHERE [ParentSystemId] IS NULL AND [CalculatorType] IS NULL;

-- 2b. Child systems now inheriting a discard pattern or participation from a parent.
--     pre-swap.sql filled in every such system that existed then; anything listed here was
--     saved since. Check with the club whether it should inherit, or set "0" if it was
--     created by the old code in the minutes before the swap.
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
SELECT s.[Id], s.[Name], c.[Name] AS Club, s.[DiscardPattern], s.[ParticipationPercent]
FROM [ScoringSystems] s
LEFT JOIN [Clubs] c ON c.[Id] = s.[ClubId]
WHERE s.[ParentSystemId] IS NOT NULL
    AND (
        (NULLIF(LTRIM(RTRIM(s.[DiscardPattern])), '') IS NULL
            AND EXISTS (
                SELECT 1 FROM Chain ch
                JOIN [ScoringSystems] a ON a.[Id] = ch.AncestorId
                WHERE ch.SystemId = s.[Id]
                    AND NULLIF(LTRIM(RTRIM(a.[DiscardPattern])), '') IS NOT NULL))
        OR (s.[ParticipationPercent] IS NULL
            AND EXISTS (
                SELECT 1 FROM Chain ch
                JOIN [ScoringSystems] a ON a.[Id] = ch.AncestorId
                WHERE ch.SystemId = s.[Id] AND a.[ParticipationPercent] IS NOT NULL)))
ORDER BY c.[Name], s.[Name];
