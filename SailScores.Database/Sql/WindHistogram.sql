DECLARE @Interval INT
set @Interval = 45
DECLARE @min INT
set @min = 0
DECLARE @max INT
set @max = 360
DECLARE @Levels INT
set @levels = 8

;
WITH ranges ([range], Center, minValue, maxValue )
AS ( SELECT 1 AS [range], 0 AS Center, convert(decimal(5,2), -22.5) AS minValue,  convert(decimal(5,2), 22.5) as MaxValue
     UNION ALL
     SELECT [range]+1, center+45 ,  convert(decimal(5,2),minValue + 45.0),  convert(decimal(5,2),MaxValue + 45.0)
     FROM ranges
     WHERE Center < 360 - 45)

SELECT r.Center,
       w.[count]
FROM ranges AS r
OUTER APPLY (
    SELECT COUNT(*) AS [count]
    FROM Weather AS w
    --- In a CROSS/OUTER APPLY, the WHERE clause works like
    --- the JOIN condition:
    INNER JOIN Races
    on w.Id = Races.WeatherId
    WHERE r.minValue <= w.WindDirectionDegrees AND
         (r.maxValue > w.WindDirectionDegrees OR
          r.Center = 0 AND
           w.WindDirectionDegrees > (360 -22.5))
           AND Races.ClubId = '23cc00a0-ba27-4fe2-8c1c-e448b1f68eba'
    ) AS w
ORDER BY r.[range];
