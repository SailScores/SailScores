using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sailscores.Core.Model;

namespace Sailscores.Core.Services
{
    public interface ICompetitorService
    {
        Task<IList<Model.Competitor>> GetCompetitorsAsync(Guid clubId);
        Task<Competitor> GetCompetitorAsync(Guid id);
        Task SaveAsync(Competitor comp);
    }
}