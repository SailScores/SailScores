using SailScores.Core.Model;
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
        Task<Competitor> GetCompetitorAsync(
            string clubInitials,
            string sailNumber);
        Task<CompetitorStatsViewModel> GetCompetitorStatsAsync(
            string clubInitials,
            string sailor);
        Task<IList<PlaceCount>> GetCompetitorSeasonRanksAsync(Guid competitorId, string seasonUrlName);
        Task<Guid?> GetCompetitorIdForSailnumberAsync(
            Guid clubId,
            string sailNumber);
        Task<IList<Competitor>> GetCompetitorsAsync(Guid clubId);
    }
}