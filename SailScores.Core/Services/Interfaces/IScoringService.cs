﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SailScores.Core.Model;

namespace SailScores.Core.Services
{
    public interface IScoringService
    {
        Task<IEnumerable<ScoreCode>> GetScoreCodesAsync(Guid clubId);
        Task<IList<ScoringSystem>> GetScoringSystemsAsync(Guid clubId, bool includeBaseSystems);
        Task<ScoringSystem> GetScoringSystemAsync(Guid scoringSystemId);
        Task<ScoringSystem> GetScoringSystemAsync(Series series);
        Task<ScoreCode> GetScoreCodeAsync(Guid id);
        Task SaveScoreCodeAsync(ScoreCode scoreCode);
        Task DeleteScoreCodeAsync(Guid id);
        Task SaveScoringSystemAsync(ScoringSystem scoringSystem);
        Task<bool> IsScoringSystemInUseAsync(Guid scoringSystemId);
        Task DeleteScoringSystemAsync(Guid systemId);
        Task<ScoringSystem> GetSiteDefaultSystemAsync();
    }
}