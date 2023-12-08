

DECLARE @NewSystemId UNIQUEIDENTIFIER = '4be8c3e8-7108-490d-934a-1f6a32e689fd'


INSERT INTO ScoringSystems
([Id]
      ,[ClubId]
      ,[Name]
      ,[DiscardPattern]
      ,[ParentSystemId]
      ,[ParticipationPercent]
      ,[IsSiteDefault]
      )
      VALUES
      (
        @NewSystemId,
        NULL,
        'Low Point Average',
        '0,1',
        NULL,
        0,
        0
      )

INSERT INTO ScoreCodes
(
     [Id]
      ,[Name]
      ,[Description]
      ,[PreserveResult]
      ,[Discardable]
      ,[Started]
      ,[FormulaValue]
      ,[AdjustOtherScores]
      ,[CameToStart]
      ,[Finished]
      ,[Formula]
      ,[ScoreLike]
      ,[ScoringSystemId]
)
Select 
     NewId()
      ,[Name]
      ,[Description]
      ,[PreserveResult]
      ,[Discardable]
      ,[Started]
      ,[FormulaValue]
      ,[AdjustOtherScores]
      ,[CameToStart]
      ,[Finished]
      ,[Formula]
      ,[ScoreLike]
      ,@NewSystemId
FROM ScoreCodes
where ScoringSystemId = '651a3372-9c45-4d07-884f-1320a9554267'