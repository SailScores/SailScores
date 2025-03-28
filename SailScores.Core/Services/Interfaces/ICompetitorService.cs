using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SailScores.Api.Dtos;
using SailScores.Core.Model;

namespace SailScores.Core.Services
{
    public interface ICompetitorService
    {
        Task<IList<Model.Competitor>> GetCompetitorsAsync(Guid clubId, Guid? fleetId, bool includeInactive);
        Task<Competitor> GetCompetitorAsync(Guid id);
        Task<Competitor> GetCompetitorByUrlNameAsync(Guid clubId, string sailor);
        Task<Competitor> GetCompetitorBySailNumberAsync(Guid clubId, String sailNumber);
        Task SaveAsync(Competitor comp);
        Task SaveAsync(CompetitorDto comp);
        Task DeleteCompetitorAsync(Guid competitorId);
        Task<IList<CompetitorSeasonStats>> GetCompetitorStatsAsync(Guid clubId, Guid competitorId);
#pragma warning disable CA1054 // Uri parameters should not be strings
        Task<IList<PlaceCount>> GetCompetitorSeasonRanksAsync(Guid competitorId, string seasonUrlName);
#pragma warning restore CA1054 // Uri parameters should not be strings
        Task<IList<Database.Entities.DeletableInfo>> GetDeletableInfo(Guid clubId);
        Task<Dictionary<Guid, DateTime?>> GetLastActiveDates(Guid clubId);
        Task<Dictionary<String, IEnumerable<Competitor>>> GetCompetitorsForFleetAsync(Guid clubId, Guid fleetId);
        Task<Dictionary<String, IEnumerable<Competitor>>> GetCompetitorsForRegattaAsync(Guid clubId, Guid regattaId);
    }
}