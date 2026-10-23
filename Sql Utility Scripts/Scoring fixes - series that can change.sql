-- Read-only. Before deploying the scoring quirk fixes (the "Fix ..." commits of October 2026), lists the
-- series or systems whose results can change when recalculated. Each query over-counts: it
-- finds series where the fixed behavior can matter, not only those that will move.
-- Run each query on its own, or the whole file.

-- 1. Formula names that aren't upper case (FIX, MAN and TIE now work in any case).
SELECT Formula, COUNT(*) AS Codes FROM ScoreCodes
WHERE Formula COLLATE Latin1_General_CS_AS <> UPPER(Formula)
GROUP BY Formula;

-- 2. Cox-Sprague: non-discardable codes are no longer discarded.

WITH SeriesSystem AS (
    -- Regatta's system, else the series', else the season default, else the club default.
    SELECT s.Id AS SeriesId, s.ClubId,
        COALESCE(
            (SELECT TOP 1 r.ScoringSystemId FROM RegattaSeries rs JOIN Regattas r ON r.Id = rs.RegattaId
             WHERE rs.SeriesId = s.Id),
            s.ScoringSystemId, se.DefaultScoringSystemId, c.DefaultScoringSystemId) AS SystemId
    FROM Series s
    JOIN Seasons se ON se.Id = s.SeasonId
    JOIN Clubs c ON c.Id = s.ClubId
),
Chain AS (
    -- Each system with its ancestors, up to the root.
    SELECT ss.Id AS SystemId, ss.Id AS AncestorId, ss.ParentSystemId, ss.CalculatorType, 0 AS Depth
    FROM ScoringSystems ss
    UNION ALL
    SELECT ch.SystemId, p.Id, p.ParentSystemId, p.CalculatorType, ch.Depth + 1
    FROM Chain ch JOIN ScoringSystems p ON p.Id = ch.ParentSystemId
),
CoxSpragueSystems AS (
    SELECT DISTINCT SystemId FROM Chain WHERE ParentSystemId IS NULL AND CalculatorType = 9
),
NonDiscardableCodes AS (
    -- A child's code overrides an ancestor's code of the same name: the nearest one wins.
    SELECT SystemId, Name FROM (
        SELECT ch.SystemId, sc.Name, sc.Discardable,
            ROW_NUMBER() OVER (PARTITION BY ch.SystemId, sc.Name ORDER BY ch.Depth) AS Nearest
        FROM Chain ch JOIN ScoreCodes sc ON sc.ScoringSystemId = ch.AncestorId
        WHERE ch.SystemId IN (SELECT SystemId FROM CoxSpragueSystems)
    ) x
    WHERE Nearest = 1 AND Discardable = 0
)
SELECT cl.Initials, se.Name AS Season, s.Name AS Series, s.Id AS SeriesId,
    COUNT(*) AS NonDiscardableScores, MAX(r.Date) AS LastRace
FROM SeriesSystem sys
JOIN NonDiscardableCodes nd ON nd.SystemId = sys.SystemId
JOIN SeriesRace sr ON sr.SeriesId = sys.SeriesId
JOIN Races r ON r.Id = sr.RaceId
JOIN Scores sc ON sc.RaceId = r.Id AND sc.Code = nd.Name
JOIN Series s ON s.Id = sys.SeriesId
JOIN Seasons se ON se.Id = s.SeasonId
JOIN Clubs cl ON cl.Id = s.ClubId
GROUP BY cl.Initials, se.Name, s.Name, s.Id
ORDER BY MAX(r.Date) DESC
OPTION (MAXRECURSION 50);

-- 3. Score code groups: inherited groups now apply without a group of the system's own.
WITH Chain AS (
    SELECT ss.Id AS SystemId, ss.ParentSystemId AS AncestorId, 1 AS Depth
    FROM ScoringSystems ss WHERE ss.ParentSystemId IS NOT NULL
    UNION ALL
    SELECT ch.SystemId, p.ParentSystemId, ch.Depth + 1
    FROM Chain ch JOIN ScoringSystems p ON p.Id = ch.AncestorId
    WHERE p.ParentSystemId IS NOT NULL
)
SELECT c.Initials, ss.Name AS SystemName, ss.Id AS SystemId,
    STRING_AGG(g.Name, ', ') AS InheritedGroups
FROM ScoringSystems ss
JOIN Chain ch ON ch.SystemId = ss.Id
JOIN ScoreCodeGroups g ON g.ScoringSystemId = ch.AncestorId
LEFT JOIN Clubs c ON c.Id = ss.ClubId
WHERE NOT EXISTS (SELECT 1 FROM ScoreCodeGroups own WHERE own.ScoringSystemId = ss.Id)
GROUP BY c.Initials, ss.Name, ss.Id
ORDER BY c.Initials, ss.Name
OPTION (MAXRECURSION 50);

-- 4. Low point averages: fleet series with full race scores now use the whole-race place.
WITH SeriesSystem AS (
    SELECT s.Id AS SeriesId,
        COALESCE(
            (SELECT TOP 1 r.ScoringSystemId FROM RegattaSeries rs JOIN Regattas r ON r.Id = rs.RegattaId
             WHERE rs.SeriesId = s.Id),
            s.ScoringSystemId, se.DefaultScoringSystemId, c.DefaultScoringSystemId) AS SystemId
    FROM Series s
    JOIN Seasons se ON se.Id = s.SeasonId
    JOIN Clubs c ON c.Id = s.ClubId
    WHERE s.FleetId IS NOT NULL AND s.UseFullRaceScores = 1
),
Chain AS (
    SELECT ss.Id AS SystemId, ss.ParentSystemId, ss.CalculatorType
    FROM ScoringSystems ss
    UNION ALL
    SELECT ch.SystemId, p.ParentSystemId, p.CalculatorType
    FROM Chain ch JOIN ScoringSystems p ON p.Id = ch.ParentSystemId
)
SELECT cl.Initials, se.Name AS Season, s.Name AS Series, s.Id AS SeriesId, ch.CalculatorType
FROM SeriesSystem sys
JOIN Chain ch ON ch.SystemId = sys.SystemId AND ch.ParentSystemId IS NULL AND ch.CalculatorType IN (6, 7, 14)
JOIN Series s ON s.Id = sys.SeriesId
JOIN Seasons se ON se.Id = s.SeasonId
JOIN Clubs cl ON cl.Id = s.ClubId
ORDER BY cl.Initials, se.Name, s.Name
OPTION (MAXRECURSION 50);

-- 5. PLC% with DNF as SER+: the penalty now comes from the DNF score.
WITH SeriesSystem AS (
    SELECT s.Id AS SeriesId,
        COALESCE(
            (SELECT TOP 1 r.ScoringSystemId FROM RegattaSeries rs JOIN Regattas r ON r.Id = rs.RegattaId
             WHERE rs.SeriesId = s.Id),
            s.ScoringSystemId, se.DefaultScoringSystemId, c.DefaultScoringSystemId) AS SystemId
    FROM Series s
    JOIN Seasons se ON se.Id = s.SeasonId
    JOIN Clubs c ON c.Id = s.ClubId
),
Chain AS (
    SELECT ss.Id AS SystemId, ss.Id AS AncestorId, ss.ParentSystemId, 0 AS Depth
    FROM ScoringSystems ss
    UNION ALL
    SELECT ch.SystemId, p.Id, p.ParentSystemId, ch.Depth + 1
    FROM Chain ch JOIN ScoringSystems p ON p.Id = ch.ParentSystemId
),
Codes AS (
    -- Each system's effective codes: the nearest definition of each name.
    SELECT SystemId, Name, Formula FROM (
        SELECT ch.SystemId, sc.Name, sc.Formula,
            ROW_NUMBER() OVER (PARTITION BY ch.SystemId, sc.Name ORDER BY ch.Depth) AS Nearest
        FROM Chain ch JOIN ScoreCodes sc ON sc.ScoringSystemId = ch.AncestorId
    ) x WHERE Nearest = 1
),
SeriesBoats AS (
    SELECT sr.SeriesId, COUNT(DISTINCT sc.CompetitorId) AS Boats
    FROM SeriesRace sr JOIN Scores sc ON sc.RaceId = sr.RaceId
    GROUP BY sr.SeriesId
)
SELECT DISTINCT cl.Initials, se.Name AS Season, s.Name AS Series, s.Id AS SeriesId
FROM SeriesSystem sys
JOIN Codes dnf ON dnf.SystemId = sys.SystemId AND dnf.Name = 'DNF' AND dnf.Formula = 'SER+'
JOIN SeriesRace sr ON sr.SeriesId = sys.SeriesId
JOIN Scores sc ON sc.RaceId = sr.RaceId
JOIN Codes plc ON plc.SystemId = sys.SystemId AND plc.Name = sc.Code AND plc.Formula = 'PLC%'
JOIN SeriesBoats sb ON sb.SeriesId = sys.SeriesId
JOIN Series s ON s.Id = sys.SeriesId
JOIN Seasons se ON se.Id = s.SeasonId
JOIN Clubs cl ON cl.Id = s.ClubId
WHERE (SELECT COUNT(*) FROM Scores x WHERE x.RaceId = sr.RaceId) < sb.Boats
ORDER BY cl.Initials, se.Name, s.Name
OPTION (MAXRECURSION 50);

-- 6. Appendix A, First = 0: scores of 0 count in ranking, A8.1 and averages.
WITH SeriesSystem AS (
    SELECT s.Id AS SeriesId,
        COALESCE(
            (SELECT TOP 1 r.ScoringSystemId FROM RegattaSeries rs JOIN Regattas r ON r.Id = rs.RegattaId
             WHERE rs.SeriesId = s.Id),
            s.ScoringSystemId, se.DefaultScoringSystemId, c.DefaultScoringSystemId) AS SystemId
    FROM Series s
    JOIN Seasons se ON se.Id = s.SeasonId
    JOIN Clubs c ON c.Id = s.ClubId
),
Chain AS (
    SELECT ss.Id AS SystemId, ss.Id AS AncestorId, ss.ParentSystemId, ss.CalculatorType
    FROM ScoringSystems ss
    UNION ALL
    SELECT ch.SystemId, p.Id, p.ParentSystemId, p.CalculatorType
    FROM Chain ch JOIN ScoringSystems p ON p.Id = ch.ParentSystemId
)
SELECT cl.Initials, se.Name AS Season, s.Name AS Series, s.Id AS SeriesId,
    CASE WHEN EXISTS (
        SELECT 1 FROM SeriesRace sr JOIN Scores sc ON sc.RaceId = sr.RaceId
        JOIN Chain c2 ON c2.SystemId = sys.SystemId
        JOIN ScoreCodes code ON code.ScoringSystemId = c2.AncestorId AND code.Name = sc.Code
        WHERE sr.SeriesId = s.Id AND code.Formula LIKE 'AVE%') THEN 1 ELSE 0 END AS UsesAverageCode
FROM SeriesSystem sys
JOIN Chain ch ON ch.SystemId = sys.SystemId AND ch.ParentSystemId IS NULL AND ch.CalculatorType = 3
JOIN Series s ON s.Id = sys.SeriesId
JOIN Seasons se ON se.Id = s.SeasonId
JOIN Clubs cl ON cl.Id = s.ClubId
ORDER BY cl.Initials, se.Name, s.Name
OPTION (MAXRECURSION 50);

-- 7. COD codes now score like the code named in ScoreLike. Systems with a COD code:
SELECT cl.Initials, ss.Name AS SystemName, sc.Name AS Code, sc.ScoreLike
FROM ScoreCodes sc
JOIN ScoringSystems ss ON ss.Id = sc.ScoringSystemId
LEFT JOIN Clubs cl ON cl.Id = ss.ClubId
WHERE sc.Formula = 'COD'
ORDER BY cl.Initials, ss.Name, sc.Name;
