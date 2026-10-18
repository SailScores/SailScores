BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260914220142_IncreaseFleetNameLengthTo40'
)
BEGIN
    DECLARE @var nvarchar(max);
    SELECT @var = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Fleets]') AND [c].[name] = N'ShortName');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [Fleets] DROP CONSTRAINT ' + @var + ';');
    ALTER TABLE [Fleets] ALTER COLUMN [ShortName] nvarchar(40) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260914220142_IncreaseFleetNameLengthTo40'
)
BEGIN
    DECLARE @var1 nvarchar(max);
    SELECT @var1 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Fleets]') AND [c].[name] = N'NickName');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [Fleets] DROP CONSTRAINT ' + @var1 + ';');
    ALTER TABLE [Fleets] ALTER COLUMN [NickName] nvarchar(40) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260914220142_IncreaseFleetNameLengthTo40'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260914220142_IncreaseFleetNameLengthTo40', N'10.0.11');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004152815_AddScoringCalculatorType'
)
BEGIN
    ALTER TABLE [ScoringSystems] ADD [CalculatorType] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004152815_AddScoringCalculatorType'
)
BEGIN
    EXEC(N'
    UPDATE ScoringSystems SET CalculatorType =
        CASE
            WHEN Name COLLATE Latin1_General_CS_AS LIKE ''%High Point Percentage%'' THEN 8
            WHEN Name COLLATE Latin1_General_CS_AS LIKE ''%Cox-Sprague%'' THEN 9
            WHEN Name COLLATE Latin1_General_CS_AS LIKE ''%Low Point Average%''
                AND Name COLLATE Latin1_General_CS_AS LIKE ''%without DNCs%'' THEN 7
            WHEN Name COLLATE Latin1_General_CS_AS LIKE ''%Low Point Average%'' THEN 6
            WHEN Name COLLATE Latin1_General_CS_AS LIKE ''%First = 0%'' THEN 3
            WHEN Name COLLATE Latin1_General_CS_AS LIKE ''%First = %''
                AND Name COLLATE Latin1_General_CS_AS LIKE ''%.75%'' THEN 5
            WHEN Name COLLATE Latin1_General_CS_AS LIKE ''%First = %''
                AND Name COLLATE Latin1_General_CS_AS LIKE ''%.7%'' THEN 4
            WHEN Name COLLATE Latin1_General_CS_AS LIKE ''%Top X High Point%'' THEN 10
            WHEN Name COLLATE Latin1_General_CS_AS LIKE ''%Appendix A%''
                AND Name COLLATE Latin1_General_CS_AS LIKE ''%pre-2025%'' THEN 2
            WHEN Name COLLATE Latin1_General_CS_AS LIKE ''PWA Standard%'' THEN 11
            ELSE 1
        END
    WHERE ParentSystemId IS NULL;');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261004152815_AddScoringCalculatorType'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261004152815_AddScoringCalculatorType', N'10.0.11');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261005025515_AddScoringSystemSettingsAndDiscardMethod'
)
BEGIN
    ALTER TABLE [ScoringSystems] ADD [DiscardMethod] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261005025515_AddScoringSystemSettingsAndDiscardMethod'
)
BEGIN
    ALTER TABLE [ScoringSystems] ADD [Settings] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261005025515_AddScoringSystemSettingsAndDiscardMethod'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261005025515_AddScoringSystemSettingsAndDiscardMethod', N'10.0.11');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008030138_AddScoreCodeIncludeInAverage'
)
BEGIN
    ALTER TABLE [ScoreCodes] ADD [IncludeInAverage] bit NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261008030138_AddScoreCodeIncludeInAverage'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261008030138_AddScoreCodeIncludeInAverage', N'10.0.11');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012225555_AddSummaryRollupModeAndScoringSystemClubAccess'
)
BEGIN
    ALTER TABLE [Series] ADD [IncludeUnrankedChildScores] bit NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012225555_AddSummaryRollupModeAndScoringSystemClubAccess'
)
BEGIN
    ALTER TABLE [Series] ADD [SummaryRollupMode] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012225555_AddSummaryRollupModeAndScoringSystemClubAccess'
)
BEGIN
    EXEC(N'UPDATE [Series] SET [SummaryRollupMode] = 1 WHERE [ChildrenSeriesAsSingleRace] = 1');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012225555_AddSummaryRollupModeAndScoringSystemClubAccess'
)
BEGIN
    ALTER TABLE [ScoringSystems] ADD [IsRestrictedToClubs] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012225555_AddSummaryRollupModeAndScoringSystemClubAccess'
)
BEGIN
    CREATE TABLE [ScoringSystemClubAccess] (
        [ScoringSystemId] uniqueidentifier NOT NULL,
        [ClubId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_ScoringSystemClubAccess] PRIMARY KEY ([ScoringSystemId], [ClubId]),
        CONSTRAINT [FK_ScoringSystemClubAccess_Clubs_ClubId] FOREIGN KEY ([ClubId]) REFERENCES [Clubs] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_ScoringSystemClubAccess_ScoringSystems_ScoringSystemId] FOREIGN KEY ([ScoringSystemId]) REFERENCES [ScoringSystems] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012225555_AddSummaryRollupModeAndScoringSystemClubAccess'
)
BEGIN
    CREATE INDEX [IX_ScoringSystemClubAccess_ClubId] ON [ScoringSystemClubAccess] ([ClubId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012225555_AddSummaryRollupModeAndScoringSystemClubAccess'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261012225555_AddSummaryRollupModeAndScoringSystemClubAccess', N'10.0.11');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012225625_InheritDiscardPatternAndParticipation'
)
BEGIN

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
        AND EXISTS (
            SELECT 1 FROM Chain c
            JOIN [ScoringSystems] a ON a.[Id] = c.AncestorId
            WHERE c.SystemId = s.[Id]
                AND NULLIF(LTRIM(RTRIM(a.[DiscardPattern])), '') IS NOT NULL);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012225625_InheritDiscardPatternAndParticipation'
)
BEGIN

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
        CASE WHEN EXISTS (
                SELECT 1 FROM Chain c
                JOIN [ScoringSystems] a ON a.[Id] = c.AncestorId
                WHERE c.SystemId = s.[Id] AND a.[ParentSystemId] IS NULL AND a.[CalculatorType] = 10)
            THEN 5 ELSE 0 END
    FROM [ScoringSystems] s
    WHERE s.[ParticipationPercent] IS NULL
        AND EXISTS (
            SELECT 1 FROM Chain c
            JOIN [ScoringSystems] a ON a.[Id] = c.AncestorId
            WHERE c.SystemId = s.[Id] AND a.[ParticipationPercent] IS NOT NULL);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012225625_InheritDiscardPatternAndParticipation'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261012225625_InheritDiscardPatternAndParticipation', N'10.0.11');
END;

COMMIT;
GO

