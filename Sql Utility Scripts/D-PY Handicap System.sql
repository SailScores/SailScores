IF NOT EXISTS (
    SELECT 1
    FROM HandicapSystems
    WHERE ClubId IS NULL
      AND Name = 'D-PY (North American Portsmouth Yardstick)'
)
BEGIN
    INSERT INTO HandicapSystems
    (
        Id,
        ClubId,
        Name,
        ParentSystemId,
        SystemType,
        Description
    )
    VALUES
    (
        NEWID(),
        NULL,
        'D-PY (North American Portsmouth Yardstick)',
        NULL,
        5,
        'North American Portsmouth Yardstick. Corrected time = elapsed / PY × 100.'
    );
END
