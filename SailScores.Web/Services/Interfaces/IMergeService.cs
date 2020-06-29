using SailScores.Core.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SailScores.Web.Services
{
    public interface IMergeService
    {
        Task<IList<Competitor>> GetSourceOptionsFor(Guid? targetCompetitorId);
        Task<int?> GetNumberOfRaces(Guid? competitorId);
        Task<IList<Season>> GetSeasons(Guid? competitorId);
        Task Merge(Guid? targetCompetitorId, Guid? sourceCompetitorId);
    }
}