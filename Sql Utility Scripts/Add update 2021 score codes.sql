



Select * from ScoringSystems where ClubId is null
Select * from ScoreCodes where ScoringSystemId ='f6f468b3-8f5c-4b66-a052-f7dda4d71d9e'
order by Name

insert into ScoreCodes
(
    Id,
    Name,
    Description,
    PreserveResult,
    Discardable,
    [Started],
    FormulaValue,
    AdjustOtherScores,
    CameToStart,
    Finished,
    Formula,
    ScoreLike,
    ScoringSystemId
)
VALUES (
    NEWID(),
    'NSC',
    'Did not sail the course',
    0,
    1,
    1,
    1,
    1,
    1,
    0,
    'CTS+',
    null,
    '651a3372-9c45-4d07-884f-1320a9554267'
)


insert into ScoreCodes
(
    Id,
    Name,
    Description,
    PreserveResult,
    Discardable,
    [Started],
    FormulaValue,
    AdjustOtherScores,
    CameToStart,
    Finished,
    Formula,
    ScoreLike,
    ScoringSystemId
)
VALUES (
    NEWID(),
    'NSC',
    'Did not sail the course',
    0,
    1,
    1,
    1,
    1,
    1,
    0,
    'SER+',
    null,
    'f6f468b3-8f5c-4b66-a052-f7dda4d71d9e'
)


update
ScoringSystems
set Name = 'Appendix A For Series - Rule 5.3'
where Id = '651a3372-9c45-4d07-884f-1320a9554267'

update ScoringSystems
set Name = 'Appendix A For Regatta'
where Id = 'f6f468b3-8f5c-4b66-a052-f7dda4d71d9e'
