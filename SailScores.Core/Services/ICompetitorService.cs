using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SailScores.Core.Model;

namespace SailScores.Core.Services
{
    public interface ICompetitorService
    {
        Task<IList<Model.Competitor>> GetCompetitorsAsync(Guid clubId);
        Task<Competitor> GetCompetitorAsync(Guid id);
    }
}