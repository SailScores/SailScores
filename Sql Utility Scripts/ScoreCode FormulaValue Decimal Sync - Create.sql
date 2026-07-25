/*
Temporary bridge triggers for FormulaValue int <-> FormulaValue_Decimal decimal migration.
Deploy after adding [ScoreCodes].[FormulaValue_Decimal].
Remove with the paired drop script after all app instances use FormulaValue_Decimal.
*/

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER TRIGGER [dbo].[TR_ScoreCodes_FormulaValue_IntToDecimal]
ON [dbo].[ScoreCodes]
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF TRIGGER_NESTLEVEL() > 1
    BEGIN
        RETURN;
    END;

    UPDATE sc
        SET sc.[FormulaValue_Decimal] =
            CASE
                WHEN i.[FormulaValue] IS NULL THEN NULL
                ELSE CONVERT(decimal(6,3), i.[FormulaValue])
            END
    FROM [dbo].[ScoreCodes] sc
    INNER JOIN inserted i ON i.[Id] = sc.[Id]
    LEFT JOIN deleted d ON d.[Id] = i.[Id]
    WHERE i.[FormulaValue] IS NOT NULL
        OR d.[Id] IS NOT NULL;
END;
GO

CREATE OR ALTER TRIGGER [dbo].[TR_ScoreCodes_FormulaValue_DecimalToInt]
ON [dbo].[ScoreCodes]
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF TRIGGER_NESTLEVEL() > 1
    BEGIN
        RETURN;
    END;

    UPDATE sc
        SET sc.[FormulaValue] =
            CASE
                WHEN i.[FormulaValue_Decimal] IS NULL THEN NULL
                ELSE CONVERT(int, FLOOR(i.[FormulaValue_Decimal]))
            END
    FROM [dbo].[ScoreCodes] sc
    INNER JOIN inserted i ON i.[Id] = sc.[Id]
    LEFT JOIN deleted d ON d.[Id] = i.[Id]
    WHERE i.[FormulaValue_Decimal] IS NOT NULL
        OR d.[Id] IS NOT NULL;
END;
GO
