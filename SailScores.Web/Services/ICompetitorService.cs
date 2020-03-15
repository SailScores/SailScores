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
    }
}