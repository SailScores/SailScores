/*
Cleanup script for temporary bridge triggers used during ScoreCode FormulaValue migration.
Run this after all application instances are writing FormulaValue_Decimal directly.
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
