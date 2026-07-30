/*
Phase 1 bridge script for ScoreCode FormulaValue migration.
Adds FormulaValue_Decimal as decimal(6,3) and backfills from FormulaValue int.
*/

IF COL_LENGTH('dbo.ScoreCodes', 'FormulaValue_Decimal') IS NULL
BEGIN
    ALTER TABLE dbo.ScoreCodes
    ADD FormulaValue_Decimal decimal(6,3) NULL;
END;
GO

UPDATE sc
SET sc.FormulaValue_Decimal = CONVERT(decimal(6,3), sc.FormulaValue)
FROM dbo.ScoreCodes sc
WHERE sc.FormulaValue IS NOT NULL
    AND (
        sc.FormulaValue_Decimal IS NULL
        OR sc.FormulaValue_Decimal <> CONVERT(decimal(6,3), sc.FormulaValue)
    );
GO
