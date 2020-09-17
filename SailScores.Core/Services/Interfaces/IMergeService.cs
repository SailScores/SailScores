using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SailScores.Core.Model;

namespace SailScores.Core.Services
{
    public interface IMergeService
    {
        Task<IList<Competitor>> GetSourceOptionsFor(Guid targetCompetitorId);
        Task<int?> GetRaceCountFor(Guid competitorId);
        Task<IList<Season>> GetSeasonsFor(Guid competitorId);
        Task Merge(
            Guid targetCompetitorId,
            Guid sourceCompetitorId,
            string mergedBy);
    }
}