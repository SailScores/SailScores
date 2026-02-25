-- =====================================================
-- Create Performance Indexes for Competitor Details Page
-- =====================================================
-- These indexes support the optimized queries for:
-- 1. Competitor lookup by URL name (Details page)
-- 2. Chart data retrieval and aggregation
--
-- Run this script against your SailScores database
-- =====================================================

-- Index for competitor lookup by UrlName - used in Details page
-- Supports queries filtering by ClubId and UrlName
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes 
    WHERE name = 'IX_Competitors_ClubId_UrlName' 
    AND object_id = OBJECT_ID('dbo.Competitors')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_Competitors_ClubId_UrlName
    ON dbo.Competitors(ClubId, UrlName)
    INCLUDE (Id, Name, SailNumber, BoatName, IsActive, BoatClassId, UrlId)
    WITH (FILLFACTOR = 90);
    PRINT 'Created index: IX_Competitors_ClubId_UrlName';
END
ELSE
    PRINT 'Index IX_Competitors_ClubId_UrlName already exists';

-- Index for competitor lookup by UrlId - fallback lookup
-- Supports queries filtering by ClubId and UrlId
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes 
    WHERE name = 'IX_Competitors_ClubId_UrlId' 
    AND object_id = OBJECT_ID('dbo.Competitors')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_Competitors_ClubId_UrlId
    ON dbo.Competitors(ClubId, UrlId)
    INCLUDE (Id, Name, SailNumber, BoatName, IsActive, BoatClassId)
    WITH (FILLFACTOR = 90);
    PRINT 'Created index: IX_Competitors_ClubId_UrlId';
END
ELSE
    PRINT 'Index IX_Competitors_ClubId_UrlId already exists';

-- Indexes for RankCountsById.sql query - covers score filtering
-- Supports score queries filtering by CompetitorId and RaceId
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes 
    WHERE name = 'IX_Scores_CompetitorId_RaceId' 
    AND object_id = OBJECT_ID('dbo.Scores')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_Scores_CompetitorId_RaceId
    ON dbo.Scores(CompetitorId, RaceId)
    INCLUDE (Place, Code)
    WITH (FILLFACTOR = 90);
    PRINT 'Created index: IX_Scores_CompetitorId_RaceId';
END
ELSE
    PRINT 'Index IX_Scores_CompetitorId_RaceId already exists';

-- Index for race date filtering in chart queries
-- Supports race queries filtering by ClubId and Date range
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes 
    WHERE name = 'IX_Races_ClubId_Date' 
    AND object_id = OBJECT_ID('dbo.Races')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_Races_ClubId_Date
    ON dbo.Races(ClubId, Date)
    INCLUDE (Id, State)
    WITH (FILLFACTOR = 90);
    PRINT 'Created index: IX_Races_ClubId_Date';
END
ELSE
    PRINT 'Index IX_Races_ClubId_Date already exists';

PRINT '';
PRINT 'Index creation script completed.';
