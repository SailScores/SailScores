﻿using SailScores.Core.Model;
using SailScores.Web.Models.SailScores;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SailScores.Web.Services
{
    public interface ICompetitorService
    {
        Task DeleteCompetitorAsync(Guid competitorId);
        Task<Competitor> GetCompetitorAsync(Guid competitorId);
        Task SaveAsync(CompetitorWithOptionsViewModel competitor);
        Task SaveAsync(
            MultipleCompetitorsWithOptionsViewModel vm,
            Guid clubId);
        Task<Competitor> GetCompetitorAsync(string clubInitials, string sailNumber);
        Task<CompetitorStatsViewModel> GetCompetitorStatsAsync(string clubInitials, string sailNumber);
        Task<List<PlaceCount>> GetCompetitorSeasonRanksAsync(Guid competitorId, string seasonName);
        Task<Guid?> GetCompetitorIdForSailnumberAsync(Guid clubdId, string sailNumber);
    }
}