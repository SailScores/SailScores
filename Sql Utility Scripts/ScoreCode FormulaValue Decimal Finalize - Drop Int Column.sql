/*
Phase 3 finalization for ScoreCode FormulaValue migration.
Run only after all application instances are using FormulaValue_Decimal.
*/

IF OBJECT_ID(N'[dbo].[TR_ScoreCodes_FormulaValue_IntToDecimal]', N'TR') IS NOT NULL
BEGIN
    DROP TRIGGER [dbo].[TR_ScoreCodes_FormulaValue_IntToDecimal];
END;
GO

IF OBJECT_ID(N'[dbo].[TR_ScoreCodes_FormulaValue_DecimalToInt]', N'TR') IS NOT NULL
BEGIN
    DROP TRIGGER [dbo].[TR_ScoreCodes_FormulaValue_DecimalToInt];
END;
GO

IF COL_LENGTH('dbo.ScoreCodes', 'FormulaValue') IS NOT NULL
BEGIN
    ALTER TABLE dbo.ScoreCodes
    DROP COLUMN FormulaValue;
END;
GO
