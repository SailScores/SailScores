using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SailScores.Api.Dtos;
using SailScores.Core.Model;

namespace SailScores.Core.Services
{
    public interface ICompetitorService
    {
        Task<IList<Model.Competitor>> GetCompetitorsAsync(Guid clubId, Guid? fleetId);
        Task<Competitor> GetCompetitorAsync(Guid id);
        Task<Competitor> GetCompetitorBySailNumberAsync(Guid clubId, String sailNumber);
        Task SaveAsync(Competitor comp);
        Task SaveAsync(CompetitorDto comp);
        Task DeleteCompetitorAsync(Guid competitorId);
        Task<IList<CompetitorSeasonStats>> GetCompetitorStatsAsync(Guid clubId, Guid competitorId);
        Task<IList<PlaceCount>> GetCompetitorSeasonRanksAsync(Guid competitorId, string seasonUrlName);
    }
}