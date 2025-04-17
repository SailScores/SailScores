

SELECT
    c.Name AS ClubName,
    c.Initials AS ClubInitials,
    MAX(r.Date) AS LastRaceDate,
    MAX(r.UpdatedDateUtc) AS LastRaceUpdate,
    (SELECT COUNT(DISTINCT r2.Id)
    FROM
        Races r2
    WHERE r2.ClubId = c.Id AND r2.Date >= (GETDATE() - 10) AND r2.Date < GETDATE() + 1) AS RaceCount,
    (SELECT COUNT(DISTINCT s2.Id)
    FROM
        Races r2 INNER JOIN Scores s2 ON s2.RaceId = r2.Id
    WHERE r2.ClubId = c.Id AND r2.Date >= (GETDATE() - 10) AND r2.Date < GETDATE() + 1) AS ScoreCount
FROM
    Races r
    FULL OUTER JOIN Clubs c
    ON r.ClubId = c.Id
WHERE
    c.IsHidden = 0
GROUP BY
    c.Id,
    c.Name,
    c.Initials
