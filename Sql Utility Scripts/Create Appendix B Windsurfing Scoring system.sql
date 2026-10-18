-- Creates the sitewide Appendix B Windsurfing base scoring system (CalculatorType 18): RRS
-- 2025-2028 Appendix B, which changes Appendix A's discards and tiebreaks. Open to every club.
-- Safe to run more than once: skipped when it exists.
-- Rules and clause map: docs/ScoringSystems/AppendixB-Windsurfing.md.
--
-- Discards: one score from 5 races, two from 12 (B8 A2.1). Clubs change this on their own
-- child system when their NOR or SIs make a different arrangement.
--
-- Score codes are copied from the sitewide Appendix A base system that scores by A5.2 (its DNS
-- scores SER+, one more than the boats in the series), except ZFP: rule 30.2 is deleted.
-- Clubs that use A5.3 override the codes on their child system (see the doc).

SET XACT_ABORT ON;

BEGIN TRANSACTION;

DECLARE @SystemId UNIQUEIDENTIFIER = 'bcf8fd64-39a6-44dc-aa4a-5c1607382c8a';

DECLARE @SourceSystemId UNIQUEIDENTIFIER = (
    SELECT TOP 1 ss.[Id] FROM [ScoringSystems] ss
    WHERE ss.[ClubId] IS NULL AND ss.[ParentSystemId] IS NULL AND ss.[CalculatorType] = 1
        AND EXISTS (
            SELECT 1 FROM [ScoreCodes] sc
            WHERE sc.[ScoringSystemId] = ss.[Id] AND sc.[Name] = 'DNS' AND sc.[Formula] = 'SER+')
    ORDER BY ss.[Name]);

IF @SourceSystemId IS NULL
    THROW 50000, 'No sitewide Appendix A base system (CalculatorType 1) scoring DNS as SER+ found.', 1;

IF NOT EXISTS (
    SELECT 1 FROM [ScoringSystems]
    WHERE [ClubId] IS NULL AND [ParentSystemId] IS NULL AND [CalculatorType] = 18)
BEGIN
    INSERT INTO [ScoringSystems]
        ([Id], [ClubId], [Name], [DiscardPattern], [ParentSystemId], [ParticipationPercent],
         [IsSiteDefault], [CalculatorType], [DiscardMethod], [Settings], [IsRestrictedToClubs])
    VALUES
        (@SystemId, NULL, 'Appendix B Windsurfing', '0,0,0,0,1,1,1,1,1,1,1,2', NULL, NULL,
         0, 18, NULL, NULL, 0);

    INSERT INTO [ScoreCodes]
        ([Id], [Name], [Description], [PreserveResult], [Discardable], [Started], [FormulaValue],
         [FormulaValue_Decimal], [AdjustOtherScores], [CameToStart], [Finished], [Formula], [ScoreLike],
         [CountAsParticipation], [IncludeInAverage], [ScoringSystemId])
    SELECT NEWID(), sc.[Name], sc.[Description], sc.[PreserveResult], sc.[Discardable], sc.[Started],
        sc.[FormulaValue], sc.[FormulaValue_Decimal], sc.[AdjustOtherScores], sc.[CameToStart],
        sc.[Finished], sc.[Formula], sc.[ScoreLike], sc.[CountAsParticipation], sc.[IncludeInAverage], @SystemId
    FROM [ScoreCodes] sc
    WHERE sc.[ScoringSystemId] = @SourceSystemId
        AND sc.[Name] <> 'ZFP';
END

COMMIT;
